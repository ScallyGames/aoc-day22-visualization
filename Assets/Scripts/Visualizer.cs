using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DefaultNamespace;
using UnityEngine;

public class Visualizer : MonoBehaviour
{
    [SerializeField] private GameObject addPrefab;
    [SerializeField] private GameObject subtractPrefab;
    [SerializeField] private GameObject resultPrefab;
    
    // Start is called before the first frame update
    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        
        string[] lines = InputLines.lines;

        Cuboid bounds = new Cuboid(-50, -50, -50, 50, 50, 50);

        var parsed = lines
            .Select(x =>
            {
                var match = Regex.Match(x, @"(on|off) x=(?'minX'-?\d+)\.{2}(?'maxX'-?\d+),y=(?'minY'-?\d+)\.{2}(?'maxY'-?\d+),z=(?'minZ'-?\d+)\.{2}(?'maxZ'-?\d+)");
                
                bool isOn = match.Groups[1].Value == "on";
                Cuboid cuboid = new Cuboid(
                    int.Parse(match.Groups["minX"].Value),
                    int.Parse(match.Groups["minY"].Value),
                    int.Parse(match.Groups["minZ"].Value),
                    int.Parse(match.Groups["maxX"].Value),
                    int.Parse(match.Groups["maxY"].Value),
                    int.Parse(match.Groups["maxZ"].Value)
                );

                return (isOn, cuboid);
            })
            .Where(x => x.cuboid.IsWithinBounds(bounds))
            ;

        // var maxNumberOfOverlaps = parsed
        //     .SelectMany(x => parsed, (a, b) => (a, b))
        //     .Where(x => x.a != x.b && x.a.cuboid.IsOverlapping(x.b.cuboid))
        //     .GroupBy(x => x.a)
        //     .Max(x => x.Count());

        // Debug.Log(maxNumberOfOverlaps);

        HashSet<Cuboid> activeCuboids = new HashSet<Cuboid>();

        foreach(var cuboidEntry in parsed)
        {
            yield return WaitAndStep();
            
            Debug.Log("New entry " + cuboidEntry);
            if(cuboidEntry.isOn)
            {
                var newAdd =  GameObject.Instantiate(addPrefab);
                newAdd.transform.position = new Vector3(cuboidEntry.cuboid.Center.X, cuboidEntry.cuboid.Center.Y,
                    cuboidEntry.cuboid.Center.Z);
                newAdd.transform.localScale = new Vector3(cuboidEntry.cuboid.Extents.X + 1, cuboidEntry.cuboid.Extents.Y + 1,
                    cuboidEntry.cuboid.Extents.Z + 1);
                
                // cut out existing on regions
                List<Cuboid> elementsToAdd = new List<Cuboid>() { cuboidEntry.cuboid };
                foreach(var activeCuboid in activeCuboids)
                {   
                    yield return WaitAndStep();
                    bool didCut = false;
                    List<Cuboid> cutElements = new List<Cuboid>();
                    List<Cuboid> elementsToAddAfterCut = new List<Cuboid>();
                    foreach(var elementToAdd in elementsToAdd)
                    {
                        if(activeCuboid.IsOverlapping(elementToAdd))
                        {
                            elementsToAddAfterCut.AddRange(elementToAdd.Subtract(activeCuboid));
                            cutElements.Add(elementToAdd);
                            didCut = true;
                        }
                    }
                    
                    StartCoroutine(DrawElements(elementsToAddAfterCut));

                    if (didCut)
                    {
                        foreach (var cutElement in cutElements)
                        {
                            elementsToAdd.Remove(cutElement);
                        }
                        elementsToAdd.AddRange(elementsToAddAfterCut);
                    }
                }


                foreach (var elementToAdd in elementsToAdd)
                {
                    Debug.Log("Got remaining element " + elementToAdd);
                    var newObject =  GameObject.Instantiate(resultPrefab);
                    newObject.transform.position = new Vector3(elementToAdd.Center.X, elementToAdd.Center.Y,
                        elementToAdd.Center.Z);
                    newObject.transform.localScale = new Vector3(elementToAdd.Extents.X + 1, elementToAdd.Extents.Y + 1,
                        elementToAdd.Extents.Z + 1);
                    activeCuboids.Add(elementToAdd);
                }
                
            }
            else
            {
                var newRemove =  GameObject.Instantiate(subtractPrefab);
                newRemove.transform.position = new Vector3(cuboidEntry.cuboid.Center.X, cuboidEntry.cuboid.Center.Y,
                    cuboidEntry.cuboid.Center.Z);
                newRemove.transform.localScale = new Vector3(cuboidEntry.cuboid.Extents.X + 1, cuboidEntry.cuboid.Extents.Y + 1,
                    cuboidEntry.cuboid.Extents.Z + 1);
                
                // Cut shape out of active cuboids
                List<Cuboid> elementsToRemove = new List<Cuboid>();
                List<Cuboid> elementsToAdd = new List<Cuboid>();
                foreach(var activeCuboid in activeCuboids)
                {
                    if(activeCuboid.IsOverlapping(cuboidEntry.cuboid))
                    {
                        elementsToAdd.AddRange(activeCuboid.Subtract(cuboidEntry.cuboid));
                        elementsToRemove.Add(activeCuboid);
                    }
                }
                foreach(var elementToRemove in elementsToRemove)
                {
                    activeCuboids.Remove(elementToRemove);
                }
                foreach(var elementToAdd in elementsToAdd)
                {
                    activeCuboids.Add(elementToAdd);
                }
            }
            yield return new WaitForSeconds(1);
        }

        ulong onCubes = activeCuboids.Aggregate(0UL, (a, b) => a + b.Size);

        Debug.Log(onCubes);
        
    }

    private IEnumerator WaitAndStep()
    {
        yield return new WaitForEndOfFrame();
        //
        // while (!Input.GetButton("Jump")) yield return new WaitForEndOfFrame();
        //
        foreach (var addCube in GameObject.FindObjectsOfType<AddCube>())
        {
            GameObject.Destroy(addCube.gameObject);
        }
        foreach (var subtractCube in GameObject.FindObjectsOfType<SubtractCube>())
        {
            GameObject.Destroy(subtractCube.gameObject);
        }
    }

    private IEnumerator DrawElements(List<Cuboid> elementsToAddAfterCut)
    {
        yield return new WaitForSeconds(0.1f);
        foreach(var elementToAdd in elementsToAddAfterCut)
        {
            var newObject =  GameObject.Instantiate(addPrefab);
            newObject.transform.position = new Vector3(elementToAdd.Center.X, elementToAdd.Center.Y,
                elementToAdd.Center.Z);
            newObject.transform.localScale = new Vector3(elementToAdd.Extents.X + 1, elementToAdd.Extents.Y + 1,
                elementToAdd.Extents.Z + 1);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

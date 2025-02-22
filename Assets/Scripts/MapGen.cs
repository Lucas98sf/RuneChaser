﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class PoissonDiscSampling
{ //esta parte do codigo retirei de (https://github.com/SebLague/Poisson-Disc-Sampling/blob/master/Poisson%20Disc%20Sampling%20E01/PoissonDiscSampling.cs), é um script que gera pontos em uma área de forma que eles não se repitam dentro de uma área ao redor deles, para evitar que árvores e entre outros fiquem muito próximos.

  public static List<Vector2> GeneratePoints(float radius, Vector2 sampleRegionSize, int numSamplesBeforeRejection = 30)
  {
    float cellSize = radius / Mathf.Sqrt(2);

    int[,] grid = new int[Mathf.CeilToInt(sampleRegionSize.x / cellSize), Mathf.CeilToInt(sampleRegionSize.y / cellSize)];
    List<Vector2> points = new List<Vector2>();
    List<Vector2> spawnPoints = new List<Vector2>();

    spawnPoints.Add(sampleRegionSize / 2);
    while (spawnPoints.Count > 0)
    {
      int spawnIndex = Random.Range(0, spawnPoints.Count);
      Vector2 spawnCentre = spawnPoints[spawnIndex];
      bool candidateAccepted = false;

      for (int i = 0; i < numSamplesBeforeRejection; i++)
      {
        float angle = Random.value * Mathf.PI * 2;
        Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
        Vector2 candidate = spawnCentre + dir * Random.Range(radius, 2 * radius);
        if (IsValid(candidate, sampleRegionSize, cellSize, radius, points, grid))
        {
          points.Add(candidate);
          spawnPoints.Add(candidate);
          grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = points.Count;
          candidateAccepted = true;
          break;
        }
      }
      if (!candidateAccepted)
      {
        spawnPoints.RemoveAt(spawnIndex);
      }

    }

    return points;
  }

  static bool IsValid(Vector2 candidate, Vector2 sampleRegionSize, float cellSize, float radius, List<Vector2> points, int[,] grid)
  {
    if (candidate.x >= 0 && candidate.x < sampleRegionSize.x && candidate.y >= 0 && candidate.y < sampleRegionSize.y)
    {
      int cellX = (int)(candidate.x / cellSize);
      int cellY = (int)(candidate.y / cellSize);
      int searchStartX = Mathf.Max(0, cellX - 2);
      int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);
      int searchStartY = Mathf.Max(0, cellY - 2);
      int searchEndY = Mathf.Min(cellY + 2, grid.GetLength(1) - 1);

      for (int x = searchStartX; x <= searchEndX; x++)
      {
        for (int y = searchStartY; y <= searchEndY; y++)
        {
          int pointIndex = grid[x, y] - 1;
          if (pointIndex != -1)
          {
            float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;
            if (sqrDst < radius * radius)
            {
              return false;
            }
          }
        }
      }
      return true;
    }
    return false;
  }
}
public class MapGen : MonoBehaviour
{ // a partir daqui apliquei o poisson-disc-sampling acima em meu game para geração do mapa
  [Header("Options")]
  public Vector2 regionSize;
  public int rejectionSamples = 30;
  List<Vector2> Points;
  [Header("Trees, Veins, Lakes and Apples")]
  public float radiusSize;
  public GameObject TreePrefab;
  public float TreeChance;
  public GameObject IronVeinPrefab;
  public float IronVeinChance;
  public GameObject GoldVeinPrefab;
  public float GoldVeinChance;
  public GameObject ApplePrefab;
  public GameObject[] LakePrefabs;
  List<Vector2> Points2;
  [Header("Rocks and Sticks")]
  public float radiusSize2;
  public GameObject RockPrefab;
  public float RockChance;
  public GameObject StickPrefab;
  public float StickChance;
  List<Vector2> EnemiesPoints;
  [Header("Enemies")]
  public float radiusSizeEnemies;
  public GameObject EnemyPrefab;
  public Collider2D[] Colliders;
  public Collider2D[] ColliderLakes;
  public Collider2D[] SafeZone;

  void Awake()
  {
    Points = PoissonDiscSampling.GeneratePoints(radiusSize, regionSize - new Vector2(-1, -1), rejectionSamples); //árvores minérios, lagos e maçãs, com chances diferentes para cada
    Points2 = PoissonDiscSampling.GeneratePoints(radiusSize2, regionSize, rejectionSamples); //sticks e rocks
    EnemiesPoints = PoissonDiscSampling.GeneratePoints(radiusSizeEnemies, regionSize, rejectionSamples); //inimigos

    if (Points != null)
    {
      foreach (Vector2 point in Points)
      {
        if (point.x >= 1 && point.x <= 99 && point.y >= 1 && point.y <= 99 && SpawnArea(Colliders, point))
        { //para não ficarem tão próximos à muralha
          float chance = Random.value;
          if (chance <= TreeChance)
          {
            Instantiate(TreePrefab, point, transform.rotation);
          }
          else
          {
            if (chance <= TreeChance + IronVeinChance)
            {
              Instantiate(IronVeinPrefab, point, transform.rotation);
            }
            else
            {
              if (chance <= TreeChance + IronVeinChance + GoldVeinChance)
              {
                Instantiate(GoldVeinPrefab, point, transform.rotation);
              }
              else
              {
                if (!SpawnArea(ColliderLakes, point))
                {
                  Instantiate(LakePrefabs[Random.Range(0, 3)], point, transform.rotation);
                }
                else
                {
                  Instantiate(ApplePrefab, point, transform.rotation);
                }
              }
            }
          }
        }
      }
    }

    if (Points2 != null)
    {
      foreach (Vector2 point in Points2)
      {
        if (point.x >= 1 && point.x <= 99 && point.y >= 1 && point.y <= 99 && SpawnArea(Colliders, point))
        {
          if (Random.value < RockChance)
          {
            Instantiate(RockPrefab, point, transform.rotation);
          }
          else
          {
            Instantiate(StickPrefab, point, transform.rotation);
          }
        }
      }
    }

    if (EnemiesPoints != null)
    {
      foreach (Vector2 point in EnemiesPoints)
      {
        if (point.x >= 1 && point.x <= 99 && point.y >= 1 && point.y <= 99)
        {
          if (SpawnArea(SafeZone, point))
          { //área segura, proximo ao spawn do player
            Instantiate(EnemyPrefab, point, transform.rotation);
          }
        }
      }
    }
  }
  public bool SpawnArea(Collider2D[] colliders, Vector3 pos) //checa se o ponto esta dentro de um collider para prevenir spawns em areas importantes, de https://www.youtube.com/watch?v=t2Cs71rDlUg
  {

    foreach (Collider2D c in colliders)
    {
      Vector3 centerPoint = c.bounds.center;
      float width = c.bounds.extents.x;
      float height = c.bounds.extents.y;

      float leftExtent = centerPoint.x - width;
      float rightExtent = centerPoint.x + width;
      float lowerExtent = centerPoint.y - height;
      float upperExtent = centerPoint.y + height;

      if (pos.x >= leftExtent && pos.x <= rightExtent)
      {
        if (pos.y >= lowerExtent && pos.y <= upperExtent)
        {
          return false;
        }
      }
    }
    return true;
  }
}
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class PrimitiveApartmentBuilding : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField, Min(1)] private int floors = 3;
    [SerializeField, Min(1)] private int windowColumns = 7;
    [SerializeField, Min(2f)] private float width = 24f;
    [SerializeField, Min(2f)] private float depth = 12f;
    [SerializeField, Min(2f)] private float floorHeight = 3.1f;

    [Header("Details")]
    [SerializeField] private bool regenerate;
    [SerializeField] private float wallThickness = 0.35f;
    [SerializeField, Min(0.08f)] private float interiorWallThickness = 0.18f;
    [SerializeField, Min(1.2f)] private float corridorWidth = 2.35f;
    [SerializeField] private Vector3 entranceOffset = new Vector3(0f, 0f, -6.21f);

    private readonly List<GameObject> generatedObjects = new List<GameObject>();
    private readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

    private void OnEnable()
    {
        Generate();
    }

    private void OnValidate()
    {
        floors = Mathf.Max(1, floors);
        windowColumns = Mathf.Max(1, windowColumns);
        width = Mathf.Max(2f, width);
        depth = Mathf.Max(2f, depth);
        floorHeight = Mathf.Max(2f, floorHeight);

        if (regenerate)
        {
            regenerate = false;
            Generate();
        }
    }

    [ContextMenu("Regenerate Building")]
    public void Generate()
    {
        ClearGeneratedObjects();

        float totalHeight = floors * floorHeight;
        float halfWidth = width * 0.5f;
        float halfDepth = depth * 0.5f;

        Color wall = new Color(0.52f, 0.53f, 0.5f);
        Color trim = new Color(0.28f, 0.29f, 0.28f);
        Color concrete = new Color(0.39f, 0.39f, 0.37f);
        Color glass = new Color(0.08f, 0.13f, 0.16f);
        Color warmGlass = new Color(0.75f, 0.55f, 0.27f);
        Color metal = new Color(0.16f, 0.17f, 0.17f);
        Color door = new Color(0.18f, 0.13f, 0.1f);
        Color interiorWall = new Color(0.48f, 0.49f, 0.46f);
        Color floorSurface = new Color(0.27f, 0.27f, 0.25f);
        Color warning = new Color(0.78f, 0.62f, 0.25f);
        Color darkPaint = new Color(0.11f, 0.12f, 0.12f);

        AddFrontWallWithEntrance(totalHeight, halfWidth, halfDepth, wall);
        AddCube("Back Wall", new Vector3(0f, totalHeight * 0.5f, halfDepth), new Vector3(width, totalHeight, wallThickness), wall);
        AddCube("Left Wall", new Vector3(-halfWidth, totalHeight * 0.5f, 0f), new Vector3(wallThickness, totalHeight, depth), wall);
        AddCube("Right Wall", new Vector3(halfWidth, totalHeight * 0.5f, 0f), new Vector3(wallThickness, totalHeight, depth), wall);
        AddCube("Roof Slab", new Vector3(0f, totalHeight + 0.18f, 0f), new Vector3(width + 0.8f, 0.35f, depth + 0.8f), concrete);
        AddCube("Foundation", new Vector3(0f, -0.18f, 0f), new Vector3(width + 0.5f, 0.35f, depth + 0.5f), concrete);

        AddInteriorLayout(interiorWall, floorSurface, concrete, metal, warning, darkPaint);

        for (int floor = 1; floor < floors; floor++)
        {
            AddCube($"Floor Band {floor}", new Vector3(0f, floor * floorHeight, -halfDepth - 0.03f), new Vector3(width + 0.2f, 0.16f, 0.12f), trim);
            AddCube($"Back Floor Band {floor}", new Vector3(0f, floor * floorHeight, halfDepth + 0.03f), new Vector3(width + 0.2f, 0.16f, 0.12f), trim);
        }

        AddEntrance(door, trim, concrete);
        AddWindows("Front", -halfDepth - 0.2f, false, glass, warmGlass, trim);
        AddWindows("Back", halfDepth + 0.2f, true, glass, warmGlass, trim);
        AddSideWindows(-halfWidth - 0.2f, "Left", glass, trim);
        AddSideWindows(halfWidth + 0.2f, "Right", glass, trim);
        AddExteriorStaircase(concrete, metal);
    }

    private void AddFrontWallWithEntrance(float totalHeight, float halfWidth, float halfDepth, Color wallColor)
    {
        float doorwayWidth = 2.55f;
        float doorwayHeight = 2.45f;
        float sideWidth = halfWidth - doorwayWidth * 0.5f;

        AddCube("Front Wall Left Segment", new Vector3(-doorwayWidth * 0.5f - sideWidth * 0.5f, totalHeight * 0.5f, -halfDepth), new Vector3(sideWidth, totalHeight, wallThickness), wallColor);
        AddCube("Front Wall Right Segment", new Vector3(doorwayWidth * 0.5f + sideWidth * 0.5f, totalHeight * 0.5f, -halfDepth), new Vector3(sideWidth, totalHeight, wallThickness), wallColor);
        AddCube("Front Wall Above Entrance", new Vector3(0f, doorwayHeight + (totalHeight - doorwayHeight) * 0.5f, -halfDepth), new Vector3(doorwayWidth, totalHeight - doorwayHeight, wallThickness), wallColor);
    }

    private void AddInteriorLayout(Color wallColor, Color floorColor, Color concreteColor, Color metalColor, Color warningColor, Color darkPaintColor)
    {
        float halfWidth = width * 0.5f;
        float halfDepth = depth * 0.5f;
        float corridorHalf = corridorWidth * 0.5f;
        float roomWallHeight = floorHeight - 0.38f;

        for (int floorIndex = 0; floorIndex < floors; floorIndex++)
        {
            float floorBase = floorIndex * floorHeight;
            float wallCenterY = floorBase + 0.12f + roomWallHeight * 0.5f;

            AddCube($"Interior Floor {floorIndex + 1}", new Vector3(0f, floorBase + 0.02f, 0f), new Vector3(width - 0.7f, 0.08f, depth - 0.7f), floorColor);

            if (floorIndex > 0)
            {
                AddCube($"Interior Ceiling Slab {floorIndex}", new Vector3(0f, floorBase - 0.06f, 0f), new Vector3(width - 0.75f, 0.12f, depth - 0.75f), concreteColor);
            }

            AddBrokenCorridorWall(floorIndex, wallCenterY, -corridorHalf, roomWallHeight, wallColor);
            AddBrokenCorridorWall(floorIndex, wallCenterY, corridorHalf, roomWallHeight, wallColor);
            AddRoomDividers(floorIndex, wallCenterY, -halfDepth + 0.55f, -corridorHalf, roomWallHeight, wallColor);
            AddRoomDividers(floorIndex, wallCenterY, corridorHalf, halfDepth - 0.55f, roomWallHeight, wallColor);
            AddSecurityRoom(floorIndex, wallCenterY, roomWallHeight, wallColor, darkPaintColor, warningColor);
            AddStairCore(floorIndex, wallCenterY, roomWallHeight, concreteColor, metalColor);
            AddApartmentDoorMarkers(floorIndex, wallCenterY, darkPaintColor, warningColor);
        }
    }

    private void AddBrokenCorridorWall(int floorIndex, float wallCenterY, float z, float wallHeight, Color wallColor)
    {
        float halfWidth = width * 0.5f;
        float[] openings = { -7.4f, -3.4f, 2.2f, 6.7f };
        float openingWidth = 1.25f;
        float cursor = -halfWidth + 0.7f;
        float end = halfWidth - 0.7f;

        for (int i = 0; i < openings.Length; i++)
        {
            float segmentEnd = Mathf.Clamp(openings[i] - openingWidth * 0.5f, cursor, end);
            AddWallSegmentX($"Corridor Wall {floorIndex + 1}-{z}-{i}", cursor, segmentEnd, wallCenterY, z, wallHeight, wallColor);
            cursor = Mathf.Clamp(openings[i] + openingWidth * 0.5f, cursor, end);
        }

        AddWallSegmentX($"Corridor Wall {floorIndex + 1}-{z}-End", cursor, end, wallCenterY, z, wallHeight, wallColor);
    }

    private void AddRoomDividers(int floorIndex, float wallCenterY, float zMin, float zMax, float wallHeight, Color wallColor)
    {
        float[] dividerX = { -8.7f, -5.1f, -1.2f, 3.2f, 7.6f };

        for (int i = 0; i < dividerX.Length; i++)
        {
            float zCenter = (zMin + zMax) * 0.5f;
            float zLength = Mathf.Abs(zMax - zMin);
            AddCube($"Room Divider {floorIndex + 1}-{i}", new Vector3(dividerX[i], wallCenterY, zCenter), new Vector3(interiorWallThickness, wallHeight, zLength), wallColor);
        }
    }

    private void AddSecurityRoom(int floorIndex, float wallCenterY, float wallHeight, Color wallColor, Color darkPaintColor, Color warningColor)
    {
        if (floorIndex != 0)
        {
            return;
        }

        float z = -corridorWidth * 0.5f - 1.8f;
        AddCube("Security Room Back Wall", new Vector3(-9.5f, wallCenterY, z), new Vector3(3.2f, wallHeight, interiorWallThickness), wallColor);
        AddCube("Security Desk Blockout", new Vector3(-9.5f, 0.55f, -4.55f), new Vector3(2.6f, 0.8f, 0.7f), darkPaintColor);
        AddCube("Security Monitor Glow", new Vector3(-9.5f, 1.15f, -4.95f), new Vector3(1.4f, 0.55f, 0.08f), warningColor, false);
        AddCube("Security Room Sign", new Vector3(-8.5f, 2.05f, -1.36f), new Vector3(1.1f, 0.28f, 0.08f), warningColor, false);
    }

    private void AddStairCore(int floorIndex, float wallCenterY, float wallHeight, Color concreteColor, Color metalColor)
    {
        float halfWidth = width * 0.5f;
        float stairX = halfWidth - 2.7f;
        float stairZ = 0f;
        float floorBase = floorIndex * floorHeight;

        AddCube($"Stair Core Side Wall {floorIndex + 1}", new Vector3(stairX - 1.5f, wallCenterY, stairZ), new Vector3(interiorWallThickness, wallHeight, 4.8f), concreteColor);
        AddCube($"Stair Core Back Wall {floorIndex + 1}", new Vector3(stairX, wallCenterY, 2.35f), new Vector3(3f, wallHeight, interiorWallThickness), concreteColor);
        AddCube($"Stair Landing {floorIndex + 1}", new Vector3(stairX, floorBase + 0.12f, stairZ + 1.1f), new Vector3(2.6f, 0.16f, 1.8f), concreteColor);

        if (floorIndex >= floors - 1)
        {
            return;
        }

        int steps = 12;

        for (int i = 0; i < steps; i++)
        {
            float t = (i + 1f) / steps;
            float y = floorBase + t * floorHeight;
            float z = stairZ - 1.8f + i * 0.32f;
            AddCube($"Interior Stair Step {floorIndex + 1}-{i + 1}", new Vector3(stairX, y, z), new Vector3(2.2f, 0.18f, 0.34f), concreteColor);
        }

        AddCube($"Interior Stair Rail {floorIndex + 1}", new Vector3(stairX - 1.05f, floorBase + floorHeight * 0.5f + 0.55f, stairZ), new Vector3(0.1f, floorHeight, 0.1f), metalColor);
    }

    private void AddApartmentDoorMarkers(int floorIndex, float wallCenterY, Color darkPaintColor, Color warningColor)
    {
        float doorY = floorIndex * floorHeight + 1.12f;
        float[] doorX = { -7.4f, -3.4f, 2.2f, 6.7f };

        for (int i = 0; i < doorX.Length; i++)
        {
            AddCube($"Door Plate Front {floorIndex + 1}-{i + 1}", new Vector3(doorX[i], doorY, -corridorWidth * 0.5f - 0.06f), new Vector3(0.9f, 1.85f, 0.08f), darkPaintColor, false);
            AddCube($"Door Plate Back {floorIndex + 1}-{i + 1}", new Vector3(doorX[i], doorY, corridorWidth * 0.5f + 0.06f), new Vector3(0.9f, 1.85f, 0.08f), darkPaintColor, false);

            if ((floorIndex + i) % 3 == 0)
            {
                AddCube($"Door Number Glow {floorIndex + 1}-{i + 1}", new Vector3(doorX[i] + 0.25f, doorY + 0.35f, -corridorWidth * 0.5f - 0.11f), new Vector3(0.22f, 0.18f, 0.04f), warningColor, false);
            }
        }
    }

    private void AddWallSegmentX(string name, float xStart, float xEnd, float y, float z, float wallHeight, Color wallColor)
    {
        if (xEnd - xStart <= 0.05f)
        {
            return;
        }

        float xCenter = (xStart + xEnd) * 0.5f;
        AddCube(name, new Vector3(xCenter, y, z), new Vector3(xEnd - xStart, wallHeight, interiorWallThickness), wallColor);
    }

    private void AddEntrance(Color doorColor, Color trimColor, Color concreteColor)
    {
        AddCube("Entrance Door", entranceOffset + new Vector3(0f, 1.05f, -0.02f), new Vector3(2.2f, 2.1f, 0.18f), doorColor, false);
        AddCube("Entrance Door Frame Top", entranceOffset + new Vector3(0f, 2.25f, -0.04f), new Vector3(2.7f, 0.22f, 0.22f), trimColor);
        AddCube("Entrance Door Frame Left", entranceOffset + new Vector3(-1.35f, 1.1f, -0.04f), new Vector3(0.18f, 2.35f, 0.22f), trimColor);
        AddCube("Entrance Door Frame Right", entranceOffset + new Vector3(1.35f, 1.1f, -0.04f), new Vector3(0.18f, 2.35f, 0.22f), trimColor);
        AddCube("Entrance Steps Lower", entranceOffset + new Vector3(0f, 0.1f, -1.0f), new Vector3(3.2f, 0.2f, 1.2f), concreteColor);
        AddCube("Entrance Steps Upper", entranceOffset + new Vector3(0f, 0.35f, -0.55f), new Vector3(2.7f, 0.25f, 0.9f), concreteColor);
        AddCube("Entrance Awning", entranceOffset + new Vector3(0f, 2.65f, -0.6f), new Vector3(3.5f, 0.18f, 1.3f), concreteColor);
    }

    private void AddWindows(string sideName, float z, bool backSide, Color glassColor, Color warmGlassColor, Color trimColor)
    {
        float spacing = width / (windowColumns + 1);

        for (int floor = 0; floor < floors; floor++)
        {
            float y = floor * floorHeight + 1.85f;

            for (int column = 0; column < windowColumns; column++)
            {
                float x = -width * 0.5f + spacing * (column + 1);
                Color color = (floor + column) % 5 == 0 ? warmGlassColor : glassColor;
                AddWindow($"{sideName} Window {floor + 1}-{column + 1}", new Vector3(x, y, z), backSide, color, trimColor);
            }
        }
    }

    private void AddSideWindows(float x, string sideName, Color glassColor, Color trimColor)
    {
        int sideColumns = 3;
        float spacing = depth / (sideColumns + 1);

        for (int floor = 0; floor < floors; floor++)
        {
            float y = floor * floorHeight + 1.85f;

            for (int column = 0; column < sideColumns; column++)
            {
                float z = -depth * 0.5f + spacing * (column + 1);
                AddSideWindow($"{sideName} Window {floor + 1}-{column + 1}", new Vector3(x, y, z), glassColor, trimColor);
            }
        }
    }

    private void AddWindow(string name, Vector3 position, bool backSide, Color glassColor, Color trimColor)
    {
        float direction = backSide ? 1f : -1f;
        AddCube(name, position, new Vector3(1.45f, 1.15f, 0.12f), glassColor, false);
        AddCube($"{name} Sill", position + new Vector3(0f, -0.72f, direction * 0.02f), new Vector3(1.7f, 0.16f, 0.2f), trimColor, false);
        AddCube($"{name} Divider", position + new Vector3(0f, 0f, direction * 0.02f), new Vector3(0.08f, 1.15f, 0.16f), trimColor, false);
    }

    private void AddSideWindow(string name, Vector3 position, Color glassColor, Color trimColor)
    {
        AddCube(name, position, new Vector3(0.12f, 1.15f, 1.45f), glassColor, false);
        AddCube($"{name} Sill", position + new Vector3(0f, -0.72f, 0f), new Vector3(0.2f, 0.16f, 1.7f), trimColor, false);
        AddCube($"{name} Divider", position, new Vector3(0.16f, 1.15f, 0.08f), trimColor, false);
    }

    private void AddExteriorStaircase(Color concreteColor, Color metalColor)
    {
        float x = width * 0.5f + 2.2f;
        float z = -depth * 0.5f + 2.2f;
        float stepWidth = 2.2f;
        float stepDepth = 0.55f;
        float stepHeight = 0.22f;

        for (int floor = 0; floor < floors; floor++)
        {
            float platformY = floor * floorHeight + 0.08f;
            AddCube($"Stair Platform {floor + 1}", new Vector3(x, platformY, z), new Vector3(stepWidth + 0.6f, 0.16f, 2.1f), concreteColor);
            AddCube($"Stair Railing Outer {floor + 1}", new Vector3(x + 1.35f, platformY + 0.65f, z), new Vector3(0.12f, 1.1f, 2.2f), metalColor);

            if (floor >= floors - 1)
            {
                continue;
            }

            int steps = 14;

            for (int i = 0; i < steps; i++)
            {
                float t = (i + 1f) / steps;
                float y = floor * floorHeight + t * floorHeight;
                float stairZ = z + 1.1f + i * stepDepth;
                AddCube($"Stair Step {floor + 1}-{i + 1}", new Vector3(x, y, stairZ), new Vector3(stepWidth, stepHeight, stepDepth), concreteColor);
            }

            AddCube($"Stair Railing Run {floor + 1}", new Vector3(x + 1.2f, floor * floorHeight + floorHeight * 0.5f + 0.75f, z + 4.6f), new Vector3(0.12f, floorHeight + 0.6f, 0.12f), metalColor);
        }
    }

    private GameObject AddCube(string objectName, Vector3 localPosition, Vector3 localScale, Color color, bool includeCollider = true)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.SetParent(transform, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = localScale;
        cube.GetComponent<Renderer>().sharedMaterial = GetMaterial(color);

        if (!includeCollider && cube.TryGetComponent(out Collider collider))
        {
            DestroyGeneratedObject(collider);
        }

        generatedObjects.Add(cube);
        return cube;
    }

    private Material GetMaterial(Color color)
    {
        if (materials.TryGetValue(color, out Material material))
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        material = new Material(shader)
        {
            color = color,
            hideFlags = HideFlags.DontSave
        };

        materials[color] = material;
        return material;
    }

    private void ClearGeneratedObjects()
    {
        generatedObjects.RemoveAll(item => item == null);

        for (int i = generatedObjects.Count - 1; i >= 0; i--)
        {
            DestroyGeneratedObject(generatedObjects[i]);
        }

        generatedObjects.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (child != null)
            {
                DestroyGeneratedObject(child.gameObject);
            }
        }
    }

    private static void DestroyGeneratedObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}

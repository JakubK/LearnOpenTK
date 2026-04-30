using OpenTK.Mathematics;

namespace LearnOpenTK;

public class GameLevel
{
    public List<GameObject> Bricks = new();
    

    public void Load(string file, int levelWidth, int levelHeight)
    {
        Bricks.Clear();

        var lines = File.ReadAllLines(file);
        List<List<int>> tileData = new();
        
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line))
            {
                break;
            }

            var row = new List<int>();
            foreach (var c in line.Split(" "))
            {
                if (int.TryParse(c, out var result))
                {
                    row.Add(result);
                }
            }
            tileData.Add(row);
        }

        if (tileData.Count > 0)
        {
            Init(tileData, levelWidth, levelHeight);
        }
    }

    public void Draw(SpriteRenderer renderer)
    {
        foreach (var gameObject in Bricks)
        {
            if (!gameObject.IsDestroyed)
            {
                gameObject.Draw(renderer);
            }
        }
    }

    public bool IsCompleted()
    {
        foreach (var gameObject in Bricks)
        {
            if (!gameObject.IsSolid && !gameObject.IsDestroyed)
            {
                return false;
            }
        }

        return true;
    }

    private void Init(List<List<int>> tileData, int levelWidth, int levelHeight)
    {
        var height = tileData.Count;
        var width = tileData[0].Count;

        var unitWidth = levelWidth / (float)width;
        var unitHeight = levelHeight / (float)height;

        var size = new Vector2(unitWidth, unitHeight);
        
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                // check block type from level data (2D level array)
                if (tileData[y][x] == 1) // solid
                {
                    var pos = new Vector2(unitWidth * x, unitHeight * y);
                    var obj = new GameObject(pos, size, Vector2.Zero, new Vector3(0.8f, 0.8f, 0.7f), ResourceManager.GetTexture("block_solid"));
                    obj.IsSolid = true;
                    Bricks.Add(obj);
                }
                else if (tileData[y][x] > 1) // non-solid; now determine its color based on level data
                {
                    var color = Vector3.One; // original: white
                    if (tileData[y][x] == 2)
                    {
                        color = new Vector3(0.2f, 0.6f, 1.0f);
                    } else if (tileData[y][x] == 3)
                    {
                        color = new Vector3(0.0f, 0.7f, 0.0f);
                    } else if (tileData[y][x] == 4)
                    {
                        color = new Vector3(0.8f, 0.8f, 0.4f);
                    } else if (tileData[y][x] == 5)
                    {
                        color = new Vector3(1.0f, 0.5f, 0.0f);
                    }
                    var pos = new Vector2(unitWidth * x, unitHeight * y);
                    var obj = new GameObject(pos, size, Vector2.Zero, color, ResourceManager.GetTexture("block"));
                    Bricks.Add(obj);
                }
            }
        }
    }
}
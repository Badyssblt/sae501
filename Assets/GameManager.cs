using UnityEngine;

public class GameManager : MonoBehaviour
{
    public RecipeData[] recipes;
    public static GameManager Instance;
    private void Awake()
    {
        Instance = this;
    }
}

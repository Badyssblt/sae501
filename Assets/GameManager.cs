using UnityEngine;

public class GameManager : MonoBehaviour
{
    public RecipeData[] recipes;
    public static GameManager Instance;
    private void Start()
    {
        Instance = this;
    }
}

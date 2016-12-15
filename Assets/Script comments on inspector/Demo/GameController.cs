using UnityEngine;

// Comment from PARENT class :D
public class GameController : MonoBehaviour
{
    /// <summary>
    /// Prefab of the player... haha so obvious
    /// </summary>
    public GameObject playerPrefab;

    /// <summary>
    /// -1: infinit
    /// </summary>
    public int maxDeaths = 5;

    [SerializeField] string anotherVariable;

    /// <summary>
    /// Variable with default value
    /// </summary>
    [SerializeField] string variableWithDefaultValue = "defaultValue";

    // quack
    // audhawwu       
    [SerializeField] string notXml;

    // audhawwu 

    // pudim
    // Joaquina
    [SerializeField] string notXmlWithDefaultValue = "pudim";
}
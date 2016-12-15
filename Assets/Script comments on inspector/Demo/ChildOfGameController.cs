using UnityEngine;

// Comment from child class
public class ChildOfGameController : GameController
{
    /// <summary>
    /// OOP? no problem
    /// </summary>
    public Vector2 childVariable;


    /// <summary>
    /// Another useless var
    /// </summary>
    public string blabla;

    // Here will work
    public NeastedClass neastedClass;

    [System.Serializable]
    public class NeastedClass
    {
        // :(
        public string thisNotWork;
    }
}
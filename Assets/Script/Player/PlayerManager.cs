using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager instance;

    public Player palyer;

    void Awake()
    {
        if (!instance)
        {
            DontDestroyOnLoad(instance);
        }
        else if(instance != this)
        {
            Destroy(instance);
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToMain : MonoBehaviour
{
    public void GoBackToMain()
    {
        SceneManager.LoadScene("Main Scene");  
    }
}

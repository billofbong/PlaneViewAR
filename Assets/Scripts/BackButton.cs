using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackButton : MonoBehaviour
{
    public Button button;

    private void Start()
    {
        Button btn = button.GetComponent<Button>();
        btn.onClick.AddListener(Click);
    }
    public void Click()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main Scene");
    }
}

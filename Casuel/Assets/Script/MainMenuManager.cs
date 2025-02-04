using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuMagager : MonoBehaviour
{
    [SerializeField] private GameObject _MainMenu;
    [SerializeField] private GameObject _LevelSelect;
    public void QuitOut()
    {
        Application.Quit();
    }
    public void MainMenu()
    {
        _MainMenu.SetActive(true);
        _LevelSelect.SetActive(false);
    }
    public void LevelSelect()
    {
        _MainMenu.SetActive(false);
        _LevelSelect.SetActive(true);
    }

    public void EnterLevel(int level)
    {
        SceneManager.LoadScene(level +1);

    }
}

using Unity.VisualScripting;

public interface IMenuController
{
    void OnEnter();
    void OnExit();
    void OnPause();
    void OnResume();
}
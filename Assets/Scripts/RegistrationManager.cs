using UnityEngine;

public class RegistrationManager : MonoBehaviour
{
    [SerializeField] private GameObject registrationPanel;

    private void Start()
    {
        if (PersistenceManager.Instance.UserService.IsRegistered)
        {
            OnUserLoaded();
        }
        else
        {
            ShowRegistration();
        }
    }

    private void ShowRegistration()
    {
        registrationPanel.SetActive(true);
    }

    private void OnUserLoaded()
    {
        registrationPanel.SetActive(false);

        // TODO:
        // Inicializar el resto de la aplicación
        // Mostrar menú principal
        // Cargar escena, etc.
    }
}
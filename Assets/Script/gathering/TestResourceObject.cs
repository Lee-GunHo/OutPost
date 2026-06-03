using UnityEngine;

public class TestResourceObject : MonoBehaviour, IInteractable
{
    [SerializeField] private string resourceName = "Stone";
    [SerializeField] private int resourceAmount = 1;

    public void Interact(PlayerPresenter playerPresenter)
    {
        Debug.Log(resourceName + " 수집: " + resourceAmount);
        Destroy(gameObject);
    }
}
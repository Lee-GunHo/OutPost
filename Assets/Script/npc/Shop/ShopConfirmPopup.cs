using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopConfirmPopup : MonoBehaviour
{
    [Header("전체 패널")]
    [SerializeField] private GameObject panel;

    [Header("텍스트")]
    [SerializeField] private TMP_Text messageText;

    [Header("버튼")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action onConfirm;

    private void Awake()
    {
        if(panel != null)
        {
            panel.SetActive(false);
        }

        if(confirmButton != null)
        {
            confirmButton.onClick.AddListener(Confirm);
        }

        if(cancelButton != null)
        {
            cancelButton.onClick.AddListener(Close);
        }
    }

    public void Open(string message, Action onConfirm)
    {
        this.onConfirm = onConfirm;

        if(messageText != null)
        {
            messageText.text = message;
        }

        if(panel != null)
        {
            panel.SetActive(true);
        }
    }

    private void Confirm()
    {
        Action confirmAction = onConfirm;

        Close();

        confirmAction?.Invoke();
    }

    public void Close()
    {
        if(panel != null)
        {
            panel.SetActive(false);
        }

        onConfirm = null;
    }
}

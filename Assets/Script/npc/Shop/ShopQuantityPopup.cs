
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopQuantityPopup : MonoBehaviour
{
    [Header("전체 패널")]
    [SerializeField] private GameObject panel;

    [Header("텍스트")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text amountText;

    [Header("버튼")]
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button cancelButton;

    private int currentAmount;
    private int maxAmount;
    private Action<int> onConfirm;

    private void Awake()
    {
        if(panel != null)
        {
            panel.SetActive(false);
        }

        if(plusButton != null)
        {
            plusButton.onClick.AddListener(Increase);
        }

        if(minusButton != null)
        {
            minusButton.onClick.AddListener(Decrease);
        }

        if(sellButton != null)
        {
            sellButton.onClick.AddListener(Confirm);
        }

        if(cancelButton != null)
        {
            cancelButton.onClick.AddListener(Close);
        }
    }

    public void Open(string itemName, int maxAmount, Action<int> onconfirm)
    {
        this.maxAmount = Mathf.Max(1, maxAmount);
        this.onConfirm = onconfirm;
        currentAmount = 1;

        if(titleText != null)
        {
            titleText.text = itemName + " 몇 개를 판매하시겠습니까?";
        }

        RefreshAmountText();

        if(panel != null)
        {
            panel.SetActive(true);
        }
    }

    private void Increase()
    {
        currentAmount++;

        if(currentAmount > maxAmount)
        {
            currentAmount = maxAmount;
        }

        RefreshAmountText();
    }

    private void Decrease()
    {
        currentAmount--;

        if(currentAmount < 1)
        {
            currentAmount = 1;
        }

        RefreshAmountText();
    }

    private void Confirm()
    {
        int selectedAmount = currentAmount;

        Close();

        onConfirm?.Invoke(selectedAmount);
    }

    private void RefreshAmountText()
    {
        if(amountText != null)
        {
            amountText.text = currentAmount.ToString();
        }
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

using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ExpositionPanel : MonoBehaviour
{
    [SerializeField] private RectTransform rowsContainer;
    [SerializeField] private ExpositionRowUI rowPrefab;
    [SerializeField] private Color leftHandColor = new(0.05490196f, 0.64705884f, 0.9137255f, 1f);
    [SerializeField] private Color rightHandColor = new(0.9607843f, 0.61960787f, 0.043137256f, 1f);

    private readonly List<ExpositionRowUI> rows = new();
    [SerializeField] private TMP_Text emptyStateText;
    private bool isInitialized;

    private void Awake()
    {
        EnsureInitialized();
    }

    public void SetData(ExpositionSummary exposition)
    {
        EnsureInitialized();

        if (exposition == null
            || exposition.leftHand == null
            || exposition.rightHand == null
            || rows.Count < 6)
        {
            ShowEmptyState();
            return;
        }

        SetActive(emptyStateText, false);
        SetActive(rowsContainer, true);

        int rowIndex = 0;
        SetRow(
            rowIndex++,
            "Flexión / extensión",
            HandType.LEFT,
            exposition.leftHand.wristFlexionExtension);
        SetRow(
            rowIndex++,
            "Desviación radial / cubital",
            HandType.LEFT,
            exposition.leftHand.wristRadialUlnarDeviation);
        SetRow(
            rowIndex++,
            "Pronación / supinación",
            HandType.LEFT,
            exposition.leftHand.wristPronationSupination);
        SetRow(
            rowIndex++,
            "Flexión / extensión",
            HandType.RIGHT,
            exposition.rightHand.wristFlexionExtension);
        SetRow(
            rowIndex++,
            "Desviación radial / cubital",
            HandType.RIGHT,
            exposition.rightHand.wristRadialUlnarDeviation);
        SetRow(
            rowIndex,
            "Pronación / supinación",
            HandType.RIGHT,
            exposition.rightHand.wristPronationSupination);
    }

    public void ShowEmptyState()
    {
        EnsureInitialized();
        SetActive(rowsContainer, false);
        SetActive(emptyStateText, true);
    }

    private void EnsureInitialized()
    {
        if (isInitialized)
            return;

        if (rowsContainer == null || rowPrefab == null)
        {
            Debug.LogError(
                "[ExpositionPanel] Configure el contenedor de filas y el prefab de fila.",
                this);
            isInitialized = true;
            return;
        }

        for (int i = 0; i < 6; i++)
        {
            ExpositionRowUI row = Instantiate(rowPrefab, rowsContainer);
            row.name = $"ExpositionRow_{i}";
            rows.Add(row);
        }

        isInitialized = true;
        ShowEmptyState();
    }

    private void SetRow(
        int index,
        string dimensionName,
        HandType handType,
        ExpositionDimensionSummary dimension)
    {
        if (index < 0 || index >= rows.Count || rows[index] == null)
            return;

        rows[index].SetData(
            GetHandLabel(handType) + " · " + dimensionName,
            dimension.hasReachedCumulativeExposureAlert,
            dimension.cumulativeExposureSeconds,
            handType == HandType.LEFT ? leftHandColor : rightHandColor);
    }

    private static string GetHandLabel(HandType handType)
    {
        return handType == HandType.LEFT ? "Izq." : "Der.";
    }   

    private static void SetActive(Component component, bool active)
    {
        if (component != null && component.gameObject.activeSelf != active)
            component.gameObject.SetActive(active);
    }

}

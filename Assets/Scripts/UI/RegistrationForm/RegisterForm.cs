using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

public class RegisterForm : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameField;
    [SerializeField] private TMP_Dropdown ddYear;
    [SerializeField] private TMP_Dropdown ddMonth;
    [SerializeField] private TMP_Dropdown ddDay;

    private const int MinYear = 1900;

    private void Start()
    {
        InitializeDropdowns();
    }

    private void InitializeDropdowns()
    {
        InitializeYears();
        InitializeMonths();

        // Inicializa los días según el año y mes seleccionados por defecto.
        UpdateDropdownDays();
    }

    private void InitializeYears()
    {
        List<string> years = new();

        for (int year = DateTime.Now.Year; year >= MinYear; year--)
        {
            years.Add(year.ToString());
        }

        ddYear.ClearOptions();
        ddYear.AddOptions(years);
    }

    private void InitializeMonths()
    {
        List<string> months = new();

        CultureInfo culture = new("es-ES");

        for (int month = 1; month <= 12; month++)
        {
            string monthName = culture.DateTimeFormat.GetMonthName(month);
            months.Add(char.ToUpper(monthName[0], culture) + monthName[1..]);
        }

        ddMonth.ClearOptions();
        ddMonth.AddOptions(months);
    }

    /// <summary>
    /// Asignar este método al evento OnValueChanged de ddYear y ddMonth.
    /// </summary>
    public void UpdateDropdownDays()
    {
        int year = GetSelectedYear();
        int month = GetSelectedMonth();

        int previousDay = Mathf.Max(1, ddDay.value + 1);

        int daysInMonth = DateTime.DaysInMonth(year, month);

        List<string> days = new();

        for (int day = 1; day <= daysInMonth; day++)
        {
            days.Add(day.ToString());
        }

        ddDay.ClearOptions();
        ddDay.AddOptions(days);

        // Si antes estaba seleccionado el 31 y ahora el mes tiene 30,
        // conserva el día más cercano posible.
        ddDay.value = Mathf.Min(previousDay, daysInMonth) - 1;
        ddDay.RefreshShownValue();
    }

    public DateTime GetBirthDate()
    {
        return new DateTime(
            GetSelectedYear(),
            GetSelectedMonth(),
            GetSelectedDay());
    }

    private int GetSelectedYear()
    {
        return int.Parse(ddYear.options[ddYear.value].text);
    }

    private int GetSelectedMonth()
    {
        return ddMonth.value + 1;
    }

    private int GetSelectedDay()
    {
        return ddDay.value + 1;
    }

    public void HandleSubmit()
    {
        string userName = nameField.text.Trim();

        if (string.IsNullOrWhiteSpace(userName))
        {
            Debug.LogWarning("Debe ingresar un nombre.");
            return;
        }

        PersistenceManager.Instance.UserService.Register(
            userName,
            GetBirthDate());
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

// Catalogo de iconos 2D por tipo de pieza. Una entrada sin sprite asignado
// hace que la UI de progreso use su sprite de fallback (blanco).
[CreateAssetMenu(
    fileName = "PieceIconCatalog",
    menuName = "Hands/Piece Icon Catalog")]
public sealed class PieceIconCatalog : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        [SerializeField] private SlotType slotType;
        [SerializeField] private Sprite sprite;

        public readonly SlotType SlotType => slotType;
        public readonly Sprite Sprite => sprite;
    }

    [Tooltip("Icono por tipo de pieza; las entradas repetidas usan la primera coincidencia.")]
    [SerializeField] private List<Entry> entries = new();

    public Sprite GetIcon(SlotType slotType)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry.SlotType == slotType && entry.Sprite != null)
                return entry.Sprite;
        }

        return null;
    }
}

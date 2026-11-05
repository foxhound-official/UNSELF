
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class EquipmentEntry
{
    [SerializeField] private string id;
    [SerializeField] private EquipmentType type = EquipmentType.Weapon;
    [SerializeField] private WeaponDefinition weapon;
    [SerializeField] private GameObject viewmodelObject;
    [SerializeField] private bool available = true;

    public string Id => id;
    public EquipmentType Type => type;
    public WeaponDefinition Weapon => weapon;
    public GameObject ViewmodelObject => viewmodelObject;
    public bool IsAvailable => available;

    public void SetAvailable(bool value)
    {
        available = value;
    }
}

public class PlayerEquipment : MonoBehaviour
{
    [Header("Available Equipment")]
    [SerializeField] private List<EquipmentEntry> equipment = new();

    private InputAction previousAction;
    private InputAction nextAction;

    private PlayerMovement playerMovement;
    private EquipmentEntry currentEntry;

    // 0 = пустые руки, 1 и далее = записи equipment.
    private int currentSlot;

    public event Action<EquipmentEntry> EquipmentChanged;

    public EquipmentEntry CurrentEntry => currentEntry;

    public EquipmentType CurrentType =>
        currentEntry != null ? currentEntry.Type : EquipmentType.Unarmed;

    public WeaponDefinition CurrentWeapon =>
        CurrentType == EquipmentType.Weapon ? currentEntry.Weapon : null;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();

        previousAction = GameControls.GetPlayerAction("Previous");
        nextAction = GameControls.GetPlayerAction("Next");

        // Игрок начинает с пустыми руками.
        currentSlot = 0;
        currentEntry = null;

        RefreshVisuals();
    }

    private void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        if (playerMovement != null && !playerMovement.enabled)
            return;

        if (previousAction.WasPressedThisFrame())
            CycleEquipment(-1);
        else if (nextAction.WasPressedThisFrame())
            CycleEquipment(1);
    }

    public void EquipUnarmed()
    {
        if (currentEntry == null)
            return;

        currentSlot = 0;
        currentEntry = null;

        RefreshVisuals();
        EquipmentChanged?.Invoke(null);
    }

    public bool TryEquip(EquipmentEntry entry)
    {
        if (entry == null || !equipment.Contains(entry))
            return false;

        if (!entry.IsAvailable || entry.Type == EquipmentType.Unarmed)
            return false;

        if (entry.Type == EquipmentType.Weapon && entry.Weapon == null)
            return false;

        if (currentEntry == entry)
            return true;

        currentEntry = entry;
        currentSlot = equipment.IndexOf(entry) + 1;

        RefreshVisuals();
        EquipmentChanged?.Invoke(currentEntry);

        return true;
    }

    public bool UnlockEquipment(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        foreach (EquipmentEntry entry in equipment)
        {
            if (entry.Id != id || entry.IsAvailable)
                continue;

            entry.SetAvailable(true);
            return true;
        }

        return false;
    }

    private void CycleEquipment(int direction)
    {
        int slotCount = equipment.Count + 1;
        int nextSlot = currentSlot;

        for (int i = 0; i < slotCount; i++)
        {
            nextSlot = (nextSlot + direction + slotCount) % slotCount;

            if (nextSlot == 0)
            {
                EquipUnarmed();
                return;
            }

            if (TryEquip(equipment[nextSlot - 1]))
                return;
        }
    }

    private void RefreshVisuals()
    {
        foreach (EquipmentEntry entry in equipment)
        {
            if (entry.ViewmodelObject == null)
                continue;

            entry.ViewmodelObject.SetActive(entry == currentEntry);
        }
    }
}

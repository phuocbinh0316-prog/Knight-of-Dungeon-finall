using TMPro;
using UnityEngine;

/// <summary>HUD trong màn chơi: hiện Cells, Depth và vũ khí hiện tại. Gán các TextMeshProUGUI.</summary>
public class RunHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cellsText;
    [SerializeField] private TextMeshProUGUI depthText;
    [SerializeField] private TextMeshProUGUI weaponText;

    private RunManager run;
    private WeaponHolder weapons;

    private void OnEnable()
    {
        run = RunManager.Get();
        run.onChanged += Refresh;

        var p = FindAnyObjectByType<Player>();
        if (p != null)
        {
            weapons = p.GetComponent<WeaponHolder>();
            if (weapons != null) weapons.onWeaponChanged += OnWeaponChanged;
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (run != null) run.onChanged -= Refresh;
        if (weapons != null) weapons.onWeaponChanged -= OnWeaponChanged;
    }

    private void OnWeaponChanged(WeaponData _) => Refresh();

    private void Refresh()
    {
        if (cellsText != null) cellsText.text = "Cells: " + run.CellsThisRun;
        if (depthText != null) depthText.text = "Depth " + run.Depth;
        if (weaponText != null)
            weaponText.text = (weapons != null && weapons.Current != null) ? weapons.Current.displayName : "";
    }
}

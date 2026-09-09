using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour
{
    public Image hpBar;
    public TextMeshProUGUI hpText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Refresh(int hp, int maxHP)
    {
        hpText.text = $"{hp}/{maxHP}";
        hpBar.fillAmount = (float) hp / maxHP;
    }
}

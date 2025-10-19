using TMPro;
using UnityEngine;

public class MenuPageUI : MonoBehaviour
{
    public TMP_Text userIdText;
    public TMP_Text userDescText;
    public int userIdVal;

    public void Init(Profile menuPage)
    {
        userIdText.text = menuPage.idprofiles.ToString();
        userDescText.text = menuPage.profileName;
        userIdVal = menuPage.idprofiles;
    }
}
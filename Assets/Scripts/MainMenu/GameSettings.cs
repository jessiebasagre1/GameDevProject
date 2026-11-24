using UnityEngine;

public enum Difficulty { Easy, Normal, Difficult }

// Remembers the difficulty chosen in the main menu while the game scene loads.
// It is saved with PlayerPrefs, so pressing Play directly in the game scene reuses the last choice.
public static class GameSettings
{
    const string Key = "GameDifficulty";
    static Difficulty? selected;

    public static Difficulty Selected
    {
        get
        {
            if (selected == null)
                selected = (Difficulty)PlayerPrefs.GetInt(Key, (int)Difficulty.Normal);
            return selected.Value;
        }
        set
        {
            selected = value;
            PlayerPrefs.SetInt(Key, (int)value);
            PlayerPrefs.Save();
        }
    }
}

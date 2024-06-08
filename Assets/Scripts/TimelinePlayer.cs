using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// This class is used to define a type that is used to register a cutscene in the CutsceneManage.
/// </summary>
/// <remarks>
/// This type takes a name, PlayableDirector and AudioSource to create a cutscene that can be played in the TimelinePlayer.
/// </remarks>
[System.Serializable]
public class Timeline
{
    [Tooltip("Name of the cutscene. This should be identical to the ids set for cutscene in Marathon.")]
    public string name;
    [Tooltip("Reference to the PlayableDirector that will play the cutscene.")]
    public PlayableDirector director;
    [Tooltip("Reference to the AudioSource that will play the audio of the cutscene.")]
    public AudioSource audio = null;
}

/// <summary>
/// This script is used to play cutscenes in the game when a poll is complete.
/// </summary>
/// <remarks>
/// This script adds the necessary event listeners to the PlayableDirector to play audio when the cutscene is played and to start the next cutscene when the cutscene is stopped.
/// When a poll is complete, the Marathon script will call the StartTimeline function with the correct cutscene id to start the cutscene.
/// </remarks>
public class TimelinePlayer : MonoBehaviour
{
    [Tooltip("List of cutscenes that will be played in the game.")]
    [SerializeField] private List<Timeline> cutscenes;
    [Tooltip("Reference to the Marathon script. This should be on the SceneManager GameObject.")]
    [SerializeField] private Marathon _marathon;

    /// <summary>
    /// This function adds the event listeners to the PlayableDirector.
    /// </summary>
    /// <remarks>
    /// This adds event listeners for on play and stop to the cutscenes.
    /// This allows them to play audio at the same time and start the next cutscene after it finishes.
    /// </remarks>
    void Awake()
    {
        for (int x = 0; x < cutscenes.Count; x++)
        {
            var cutscene = cutscenes[x];
            cutscene.director.played += Director_Played;
            cutscene.director.stopped += Director_Stopped;
        }
    }

    /// <summary>
    /// Once a cutscene starts, it plays the audio of the cutscene.
    /// </summary>
    /// <param name="obj">PlayableDirector. The component that contains the script animation.</param>
    private void Director_Played(PlayableDirector obj)
    {
        foreach (var cutscene in cutscenes)
        {
            if (cutscene.director == obj && cutscene.audio != null)
            {
                Debug.Log("Playing audio");
                cutscene.audio.Play();
                break;
            }
        }
        Debug.Log("Playing");
    }

    /// <summary>
    /// Once a cutscene stops, it starts the next cutscene.
    /// </summary>
    /// <param name="obj">PlayableDirector. The component that contains the script animation.</param>
    private void Director_Stopped(PlayableDirector obj)
    {
        Debug.Log("Stopped");
        _marathon.StartCutscene();
    }

    /// <summary>
    /// This function starts a cutscene with the given name.
    /// </summary>
    /// <param name="name">string. This is the name/id of a cutscene and is used to play it directly.</param>
    public void StartTimeline(string name)
    {
        foreach (var cutscene in cutscenes)
        {
            if (cutscene.name == name)
            {
                cutscene.director.Play();
            }
        }
    }
}

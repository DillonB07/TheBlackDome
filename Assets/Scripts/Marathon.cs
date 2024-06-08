using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// This class is used to define a cutscene option that will be shown to the players in a poll.
/// </summary>
/// <remarks>
/// This class is used as a type in the Marathon script to make sure the cutscene options are correctly defined inside the Unity Inspector.
/// </remarks>
[System.Serializable]
public class CutscenePoll
{
    [Tooltip("Name of the poll. This will be shown to the players")]
    public string name;
    [Tooltip("List of cutscenes that the players can vote for. The cutscene with the most votes will be shown")]
    public List<Option> cutscenes;
    [Tooltip("Amount of seconds that the poll should be accepting responses. The game is paused during this time")]
    public int duration;
}

/// <summary>
///  This script is used to define a "marathon" of cutscenes that will be shown to the players and plays them accordingly.
/// </summary>
/// <remarks>
/// This script is used to define a "marathon" of cutscenes that will be shown to the players and plays them accordingly.
/// It will show a poll to the players with the cutscenes that are defined in the CutscenePoll list.
/// The cutscene with the most votes will be shown.
/// If there is only one cutscene, it will be shown directly without a poll.
/// </remarks>
public class Marathon : MonoBehaviour
{
    [Tooltip("Reference to the SocketManager script. This should be on the SceneManager GameObject")]
    [SerializeField] private SocketManager socket;
    [Tooltip("Reference to the TimelinePlayer script. This should be on the CutsceneManager GameObject")]
    [SerializeField] private TimelinePlayer player;
    
    [Space]
    
    [Tooltip("List of polls that will be shown to the players. They'll be run in consecutive order.")]
    [SerializeField] private List<CutscenePoll> polls = new List<CutscenePoll>();
    
    private int currentPollIndex = 0;


    /// <summary>
    /// This function creates the next poll
    /// </summary>
    /// <remarks>
    /// If an index is not specified, it will create the next poll in the list.
    /// If there aren't multiple cutscenes, it will call the function to play the cutscene directly.
    /// </remarks>
    /// <param name="index">int. The id of the poll. If -1, it will play the next one</param>
    public void StartCutscene(int index = -1)
    {
        print("Starting StartCutscene. CurrentPollIndex" + currentPollIndex);
        if (index == -1)
        {
            index = currentPollIndex;
        } else if (index < 0 || index >= polls.Count)
        {
            Debug.LogError("Invalid index");
            return;
        }
        currentPollIndex++;

        var poll = polls[index];
        Debug.Log($"Starting poll {poll.name}");
        if (poll.cutscenes.Count == 1)
        {
            player.StartTimeline(poll.cutscenes[0].id);
        }
        else
        {
            socket.SendWebSocketPoll(poll.name, poll.cutscenes, poll.duration);
        }
    }

    /// <summary>
    /// This runs the cutscene for the winner of the poll.
    /// </summary>
    /// <param name="pollId">string. The id of the poll that has completed.</param>
    /// <param name="optionId">string. The winning cutscene id. This gets played</param>
    public void ReceiveResponse(string pollId, string optionId)
    {
        Debug.Log($"Received response for poll {pollId} with option {optionId}");
        player.StartTimeline(optionId);
    }

}

using UnityEngine;

public class Goal : MonoBehaviour
{
    public bool isPlayer1Goal;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            if (!isPlayer1Goal)
            {
                GameObject.Find("GameManager").GetComponent<GameManager>().Player1Scored();
                print("Player 1 pontuou!");
            }
            else
            {
                GameObject.Find("GameManager").GetComponent<GameManager>().Player2Scored();
                print("Player 2 pontuou!");
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MovingObject : MonoBehaviour {

    public float moveTime = 1.0f;
    public LayerMask blockingLayer;

    private BoxCollider2D boxCollider;
    private Rigidbody2D rb2d;
    private float inverseMoveTime;

	// protected virtual - can be overriden by implementing class
	protected virtual void Start ()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        rb2d = GetComponent<Rigidbody2D>();
        // We are doing the divide now so we can do the calculation
        // as a multiply later - more efficient computationally
        inverseMoveTime = 1f / moveTime;
	}
	
    protected bool Move (int xDir, int yDir, out RaycastHit2D hit) // out keyword - pass by ref
    {
        Vector2 start = transform.position; // implicit converesion, discard z data
        Vector2 end = start + new Vector2(xDir, yDir);

        boxCollider.enabled = false;
        hit = Physics2D.Linecast(start, end, blockingLayer); // LayerMask
        boxCollider.enabled = true;

        if (hit.transform == null)
        {
            // space is open and available to move into
            StartCoroutine(SmoothMovement(end));
            return true;
        }

        // Move was unsucessful
        return false;
    }

    // smooth movement co-routine
    protected IEnumerator SmoothMovement(Vector3 end)
    {
        // Using sqrMagnitude bc it's computationally cheaper than magnitude
        float sqrRemainingDistance = (transform.position - end).sqrMagnitude;

        while (sqrRemainingDistance > float.Epsilon) // ~ sqrRemainingDistance > 0
        {
            // Vector3.MoveTowards - Moves a point in a straigt line towards a target point
            // the return value will be (inverseMoveTime * Time.deltaTime) points closer to our destination
            Vector3 newPosition = Vector3.MoveTowards(rb2d.position, end, inverseMoveTime * Time.deltaTime);
            rb2d.MovePosition(newPosition);
            sqrRemainingDistance = (transform.position - end).sqrMagnitude;
            yield return null; // wait for a frame before re-eval the cont'd of the loop
        }
    }

    protected virtual void AttemptMove<T> (int xDir, int yDir)
        where T: Component
    {
        RaycastHit2D hit;
        bool canMove = Move(xDir, yDir, out hit);

        if (hit.transform == null)
        {
            // nothing was hit by our linecast in move
            return;
        }

        // attached to the object that was hit
        T hitComponent = hit.transform.GetComponent<T>();

        if (!canMove && hitComponent != null)
        {
            // blocked and cant move
            OnCantMove(hitComponent);
        }
    }

	protected abstract void OnCantMove<T> (T component)
        where T: Component;

}

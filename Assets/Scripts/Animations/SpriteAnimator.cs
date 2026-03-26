using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("List of sprites to animate through.")]
    public List<Sprite> sprites = new List<Sprite>();

    [Tooltip("Frames per second (animation speed).")]
    public float fps = 10f;

    [Tooltip("If true, animation starts automatically when the game starts.")]
    public bool playOnStart = true;

    [Tooltip("Frame index (0‑based) at which the event should be triggered. -1 means no trigger.")]
    public int eventTriggerFrame = 0;

    public event System.Action OnAnimationEvent;

    private SpriteRenderer spriteRenderer;
    private int currentFrame = 0;
    private float timer = 0f;
    private bool isPlaying = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (sprites == null)
            sprites = new List<Sprite>();
    }

    private void Start()
    {
        if (playOnStart && sprites.Count > 0)
            Play();
    }

    private void Update()
    {
        if (!isPlaying || sprites.Count == 0) return;

        timer += Time.deltaTime;
        float timePerFrame = 1f / fps;

        while (timer >= timePerFrame)
        {
            timer -= timePerFrame;
            MoveToNextFrame();
        }
    }
    private void MoveToNextFrame()
    {
        if (sprites.Count == 0) return;

        currentFrame++;
        if (currentFrame >= sprites.Count)
            currentFrame = 0;

        spriteRenderer.sprite = sprites[currentFrame];

        if (currentFrame == eventTriggerFrame)
            OnAnimationEvent?.Invoke();
    }
    public void Play()
    {
        if (sprites.Count == 0) return;

        currentFrame = 0;
        timer = 0f;
        spriteRenderer.sprite = sprites[0];
        isPlaying = true;
    }
    public void Pause()
    {
        isPlaying = false;
    }
    public void Resume()
    {
        if (sprites.Count == 0) return;
        isPlaying = true;
    }
    public void Restart()
    {
        if (sprites.Count == 0) return;

        currentFrame = 0;
        timer = 0f;
        spriteRenderer.sprite = sprites[0];
        isPlaying = true;
    }
}
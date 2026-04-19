using UnityEngine;
using TuringSignal.Gameplay;
using TuringSignal.Input;

namespace TuringSignal.Audio
{
    public sealed class GameAudio : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Clips")]
        [SerializeField] private AudioClip playBgm;
        [SerializeField] private AudioClip botDeath;
        [SerializeField] private AudioClip botTurn;
        [SerializeField] private AudioClip botWalk;
        [SerializeField] private AudioClip button;

        private RobotInputRouter inputRouter;
        private RobotLogic robotLogic;

        private void Start()
        {
            PlayBgmIfNeeded();
        }

        public void Initialize(RobotInputRouter inputRouter, RobotLogic robotLogic)
        {
            Unsubscribe();

            this.inputRouter = inputRouter;
            this.robotLogic = robotLogic;

            if (this.inputRouter != null)
            {
                this.inputRouter.OnRotatePressed += HandleRotatePressed;
                this.inputRouter.OnInteractPressed += HandleInteractPressed;
            }

            if (this.robotLogic != null)
            {
                this.robotLogic.OnMoveSucceeded += HandleMoveSucceeded;
            }

            PlayBgmIfNeeded();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public void PlayDeath()
        {
            PlaySfx(botDeath);
        }

        private void HandleRotatePressed()
        {
            PlaySfx(button);
            PlaySfx(botTurn);
        }

        private void HandleInteractPressed()
        {
            PlaySfx(button);
        }

        private void HandleMoveSucceeded(Vector2Int fromCell, Vector2Int toCell)
        {
            PlaySfx(botWalk);
        }

        private void PlayBgmIfNeeded()
        {
            if (bgmSource == null || playBgm == null)
            {
                return;
            }

            if (bgmSource.clip != playBgm)
            {
                bgmSource.clip = playBgm;
            }

            bgmSource.loop = true;

            if (!bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
        }

        private void PlaySfx(AudioClip clip)
        {
            if (sfxSource == null || clip == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip);
        }

        private void Unsubscribe()
        {
            if (inputRouter != null)
            {
                inputRouter.OnRotatePressed -= HandleRotatePressed;
                inputRouter.OnInteractPressed -= HandleInteractPressed;
            }

            if (robotLogic != null)
            {
                robotLogic.OnMoveSucceeded -= HandleMoveSucceeded;
            }

            inputRouter = null;
            robotLogic = null;
        }
    }
}

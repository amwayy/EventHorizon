using System;
using GameEvent;
using GameEvent.Args;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

		private bool _ignoreNextLookInput;

		private void Start()
		{
			EventComponent.Instance.Subscribe(ScreenshotModeToggleEventArgs.EventId, OnScreenshotModeToggled);
		}

		private void OnDestroy()
		{
			EventComponent.Instance.Unsubscribe(ScreenshotModeToggleEventArgs.EventId, OnScreenshotModeToggled);
		}

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}
#endif


		public virtual void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			// Ignore accumulated input after exiting screenshot mode
			if (_ignoreNextLookInput)
			{
				_ignoreNextLookInput = false;
				look = Vector2.zero;
				return;
			}

			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}
		
		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		public void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}

		private void OnScreenshotModeToggled(object sender, GameEventArgs e)
		{
			if (e is not ScreenshotModeToggleEventArgs args) return;

			var isInScreenshotMode = args.IsOn;
			cursorLocked = !isInScreenshotMode;
			SetCursorState(!isInScreenshotMode);
			cursorInputForLook = !isInScreenshotMode;

			// Always clear look input when toggling screenshot mode
			// This prevents accumulated mouse delta from causing camera jumps
			look = Vector2.zero;

			// When exiting screenshot mode, ignore the next look input frame
			// to prevent any accumulated delta from being applied
			if (!isInScreenshotMode)
			{
				_ignoreNextLookInput = true;
			}
		}
	}
	
}
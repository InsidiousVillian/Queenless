using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Queenless
{
    /// <summary>
    /// Runtime copy of Assets/Input/Queenless.inputactions.
    /// Gameplay and UI are separate maps. Space is Stay on Gameplay;
    /// the flow treats that same press as dismiss while a card is open.
    /// </summary>
    public sealed class QueenlessInput : IDisposable
    {
        public readonly InputActionAsset Asset;
        public readonly InputAction Move;
        public readonly InputAction NextBody;
        public readonly InputAction PreviousBody;
        public readonly InputAction Follow;
        public readonly InputAction Stay;
        public readonly InputAction Wait;
        public readonly InputAction Undo;
        public readonly InputAction Restart;
        public readonly InputAction Pause;
        public readonly InputAction Confirm;

        public QueenlessInput()
        {
            Asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var gameplay = Asset.AddActionMap("Gameplay");
            var ui = Asset.AddActionMap("UI");

            Move = gameplay.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            // WASD and arrows. W is also Wait; GameFlow chooses.
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            NextBody = Bind(gameplay, "NextBody", "<Keyboard>/tab", "<Keyboard>/e");
            PreviousBody = Bind(gameplay, "PreviousBody", "<Keyboard>/q");
            Follow = Bind(gameplay, "Follow", "<Keyboard>/f");
            Stay = Bind(gameplay, "Stay", "<Keyboard>/space");
            Wait = Bind(gameplay, "Wait", "<Keyboard>/w");
            Undo = Bind(gameplay, "Undo", "<Keyboard>/z", "<Keyboard>/backspace");
            Restart = Bind(gameplay, "Restart", "<Keyboard>/r");
            Pause = Bind(gameplay, "Pause", "<Keyboard>/escape");
            Confirm = Bind(ui, "Confirm", "<Keyboard>/enter", "<Keyboard>/numpadEnter");

            gameplay.Enable();
            ui.Enable();
        }

        static InputAction Bind(InputActionMap map, string name, params string[] paths)
        {
            var action = map.AddAction(name, InputActionType.Button);
            for (int i = 0; i < paths.Length; i++)
                action.AddBinding(paths[i]);
            return action;
        }

        public void Dispose()
        {
            if (Asset != null)
            {
                Asset.Disable();
                UnityEngine.Object.Destroy(Asset);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Unity.Services.Authentication.Editor.Models;
using Unity.Services.Core.Internal;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Services.Authentication.Editor
{
    class IdProviderOptions
    {
        public const string IdProviderApple = "apple.com";
        public const string IdProviderGoogle = "google.com";
        public const string IdProviderFacebook = "facebook.com";
        public const string IdProviderSteam = "steampowered.com";

        public string IdProviderType { get; set; }
        public string DisplayName { get; set; }
        public string ClientIdDisplayName { get; set; } = "Client ID";

        public string ClientSecretDisplayName { get; set; } = "Client Secret";
        public bool NeedClientSecret {get; set; }

        static readonly Dictionary<string, IdProviderOptions> s_IdProviderOptions = new Dictionary<string, IdProviderOptions>
        {
            [IdProviderApple] = new IdProviderOptions
            {
                IdProviderType = IdProviderApple,
                DisplayName = "Sign-in with Apple",
                ClientIdDisplayName = "App ID",
                NeedClientSecret = false
            },
            [IdProviderGoogle] = new IdProviderOptions
            {
                IdProviderType = IdProviderGoogle,
                DisplayName = "Google",
                ClientIdDisplayName = "Client ID",
                NeedClientSecret = false
            },
            [IdProviderFacebook] = new IdProviderOptions
            {
                IdProviderType = IdProviderFacebook,
                DisplayName = "Facebook",
                ClientIdDisplayName = "App ID",
                ClientSecretDisplayName = "App Secret",
                NeedClientSecret = true
            },
            [IdProviderSteam] = new IdProviderOptions
            {
                IdProviderType = IdProviderSteam,
                DisplayName = "Steam",
                ClientIdDisplayName = "App ID",
                ClientSecretDisplayName = "Key",
                NeedClientSecret = true
            }
        };

        public static IdProviderOptions GetOptions(string idProviderType)
        {
            if (!s_IdProviderOptions.ContainsKey(idProviderType))
            {
                return null;
            }
            return s_IdProviderOptions[idProviderType];
        }
    }

    class IdProviderElement : VisualElement
    {
        const string k_ElementUxml = "Packages/com.unity.services.authentication/Editor/UXML/IdProviderElement.uxml";

        string m_IdDomainId;
        IAuthenticationAdminClient m_AdminClient;
        IdProviderOptions m_Options;

        Foldout m_Container;
        Toggle m_Enabled;
        TextField m_ClientId;
        TextField m_ClientSecret;
        Button m_SaveButton;
        Button m_CancelButton;
        Button m_DeleteButton;

        // Whether skip the confirmation window for tests/automation.
        bool m_SkipConfirmation;

        /// <summary>
        /// The foldout container to show or hide the ID provider details.
        /// </summary>
        public Foldout Container => m_Container;

        /// <summary>
        /// The toggle to control whether the ID provider is enabled.
        /// </summary>
        public Toggle EnabledToggle => m_Enabled;

        /// <summary>
        /// The text field to fill the client ID.
        /// </summary>
        public TextField ClientIdField => m_ClientId;

        /// <summary>
        /// The text field to fill the client secret.
        /// </summary>
        public TextField ClientSecretField => m_ClientSecret;

        /// <summary>
        /// The button to save the changes.
        /// </summary>
        public Button SaveButton => m_SaveButton;

        /// <summary>
        /// The button to cancel changes.
        /// </summary>
        public Button CancelButton => m_CancelButton;


        /// <summary>
        /// The button to delete the current ID provider.
        /// </summary>
        public Button DeleteButton => m_DeleteButton;

        /// <summary>
        /// Event triggered when the <cref="IdProviderElement"/> starts or finishes waiting for an async operation.
        /// The first parameter of the callback is the sender.
        /// The second parameter is true if it starts waiting, and false if it finishes waiting.
        /// </summary>
        public event Action<IdProviderElement, bool> Waiting;

        /// <summary>
        /// Event triggered when the current <cref="IdProviderElement"/> needs to be deleted by the container.
        /// The parameter of the callback is the sender.
        /// </summary>
        public event Action<IdProviderElement> Deleted;

        /// <summary>
        /// Event triggered when the current <cref="IdProviderElement"/> catches an error.
        /// The first parameter of the callback is the sender.
        /// The second parameter is the exception caught by the element.
        /// </summary>
        public event Action<IdProviderElement, Exception> Error;

        /// <summary>
        /// The value saved on the server side.
        /// </summary>
        public IdProviderResponse SavedValue { get; set; }

        /// <summary>
        /// The value of that is about to be saved to the server.
        /// </summary>
        public IdProviderResponse CurrentValue { get; set; }

        public bool Changed =>
            SavedValue?.Type != CurrentValue?.Type ||
            SavedValue?.Disabled != CurrentValue?.Disabled ||
            (SavedValue?.ClientId ?? "") != (CurrentValue?.ClientId ?? "") ||
            (SavedValue?.ClientSecret ?? "") != (CurrentValue?.ClientSecret ?? "");

        public bool IsValid =>
            !string.IsNullOrEmpty(CurrentValue.ClientId) &&
            (!m_Options.NeedClientSecret || !string.IsNullOrEmpty(CurrentValue.ClientSecret));

        public IdProviderElement(string idDomain, IAuthenticationAdminClient adminClient, IdProviderResponse savedValue, IdProviderOptions options, bool skipConfirmation = false)
        {
            m_IdDomainId = idDomain;
            m_AdminClient = adminClient;
            m_Options = options;
            m_SkipConfirmation = skipConfirmation;

            SavedValue = savedValue;
            CurrentValue = SavedValue.Clone();

            var containerAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_ElementUxml);
            if (containerAsset != null)
            {
                var containerUI = containerAsset.CloneTree().contentContainer;

                m_Container = containerUI.Q<Foldout>(className: "auth-id-provider-details");
                m_Container.text = savedValue.Type;

                // If the ID Provider element is new, default to unfold.
                m_Container.value = SavedValue.New;

                m_Enabled = containerUI.Q<Toggle>("id-provider-enabled");
                m_Enabled.RegisterCallback<ChangeEvent<bool>>(OnEnabledChanged);

                m_ClientId = containerUI.Q<TextField>("id-provider-client-id");
                m_ClientId.label = options.ClientIdDisplayName;
                m_ClientId.RegisterCallback<ChangeEvent<string>>(OnClientIdChanged);

                m_ClientSecret = containerUI.Q<TextField>("id-provider-client-secret");
                if (options.NeedClientSecret)
                {
                    m_ClientSecret.label = options.ClientSecretDisplayName;
                    m_ClientSecret.RegisterCallback<ChangeEvent<string>>(OnClientSecretChanged);
                }
                else
                {
                    m_ClientSecret.style.display = DisplayStyle.None;
                }

                m_SaveButton = containerUI.Q<Button>("id-provider-save");
                m_SaveButton.SetEnabled(false);
                m_SaveButton.clicked += OnSaveButtonClicked;

                m_CancelButton = containerUI.Q<Button>("id-provider-cancel");
                m_CancelButton.SetEnabled(false);
                m_CancelButton.clicked += OnCancelButtonClicked;

                m_DeleteButton = containerUI.Q<Button>("id-provider-delete");
                m_DeleteButton.clicked += OnDeleteButtonClicked;
                m_DeleteButton.SetEnabled(!savedValue.New);

                ResetCurrentValue();
                Add(containerUI);
            }
            else
            {
                throw new Exception("Asset not found: " + k_ElementUxml);
            }
        }

        void RefreshButtons()
        {
            bool hasChanges = Changed;
            m_SaveButton.SetEnabled(hasChanges && IsValid);
            m_CancelButton.SetEnabled(hasChanges || SavedValue.New);
            if (SavedValue.New)
            {
                m_DeleteButton.SetEnabled(false);
                m_DeleteButton.style.display = DisplayStyle.None;
            }
            else
            {
                m_DeleteButton.SetEnabled(true);
                m_DeleteButton.style.display = DisplayStyle.Flex;
            }
        }

        void OnEnabledChanged(ChangeEvent<bool> e)
        {
            CurrentValue.Disabled = !e.newValue;
            RefreshButtons();
        }

        void OnClientIdChanged(ChangeEvent<string> e)
        {
            CurrentValue.ClientId = e.newValue;
            RefreshButtons();
        }

        void OnClientSecretChanged(ChangeEvent<string> e)
        {
            CurrentValue.ClientSecret = e.newValue;
            RefreshButtons();
        }

        void OnSaveButtonClicked()
        {
            int option = DisplayDialogComplex("Save your changes", "Do you want to save the ID provider changes?", "Save", "Cancel", "");
            switch (option)
            {
                case 0:
                    Waiting?.Invoke(this, true);

                    if (SavedValue.New)
                    {
                        var asyncOp = m_AdminClient.CreateIdProvider(m_IdDomainId, new CreateIdProviderRequest(CurrentValue));
                        asyncOp.Completed += OnSaveCompleted;
                    }
                    else
                    {
                        var body = new UpdateIdProviderRequest(CurrentValue);
                        var asyncOp = m_AdminClient.UpdateIdProvider(m_IdDomainId, CurrentValue.Type, body);
                        asyncOp.Completed += OnSaveCompleted;
                    }
                    break;

                case 1:
                    break;

                default:
                    Debug.LogError("Unrecognized option.");
                    break;
            }
        }

        void OnSaveCompleted(IAsyncOperation<IdProviderResponse> asyncOp)
        {
            if (asyncOp.Exception != null)
            {
                Error?.Invoke(this, AuthenticationSettingsHelper.ExtractException(asyncOp.Exception));
                Waiting?.Invoke(this, false);
                return;
            }

            SavedValue = asyncOp.Result;

            // Check enable/disable status
            if (SavedValue.Disabled != CurrentValue.Disabled)
            {
                SavedValue.ClientSecret = CurrentValue.ClientSecret;
                asyncOp = CurrentValue.Disabled ? m_AdminClient.DisableIdProvider(m_IdDomainId, CurrentValue.Type) : m_AdminClient.EnableIdProvider(m_IdDomainId, CurrentValue.Type);
                asyncOp.Completed += OnEnableDisableCompleted;
                return;
            }

            // Enable/disable is not changed
            ResetCurrentValue();
            RefreshButtons();
            Waiting?.Invoke(this, false);
        }

        void OnEnableDisableCompleted(IAsyncOperation<IdProviderResponse> asyncOp)
        {
            // Handle enable/disable exception
            if (asyncOp.Exception != null)
            {
                Error?.Invoke(this, AuthenticationSettingsHelper.ExtractException(asyncOp.Exception));
                Waiting?.Invoke(this, false);
                return;
            }

            // Only reset current value when no exception
            SavedValue = asyncOp.Result;
            ResetCurrentValue();
            RefreshButtons();
            Waiting?.Invoke(this, false);
        }

        void OnCancelButtonClicked()
        {
            if (SavedValue.New)
            {
                // It's a new ID provider and it hasn't been saved to the server yet.
                // Simply trigger delete event to notify parent to remove the element from the list.
                Deleted?.Invoke(this);
                return;
            }
            ResetCurrentValue();
        }

        void ResetCurrentValue()
        {
            CurrentValue = SavedValue.Clone();
            m_Enabled.value = !CurrentValue.Disabled;
            m_ClientId.value = CurrentValue.ClientId ?? "";
            m_ClientSecret.value = CurrentValue.ClientSecret ?? "";

            RefreshButtons();
        }

        void OnDeleteButtonClicked()
        {
            int option = DisplayDialogComplex("Delete Request", "Do you want to delete the ID Provider?", "Delete", "Cancel", "");
            switch (option)
            {
                // Delete
                case 0:
                    Waiting?.Invoke(this, true);
                    var asyncOp = m_AdminClient.DeleteIdProvider(m_IdDomainId, CurrentValue.Type);
                    asyncOp.Completed += OnDeleteCompleted;
                    break;

                // Cancel
                case 1:
                    break;

                default:
                    Debug.LogError("Unrecognized option.");
                    break;
            }
        }

        void OnDeleteCompleted(IAsyncOperation<IdProviderResponse> asyncOp)
        {
            if (asyncOp.Exception != null)
            {
                Error?.Invoke(this, AuthenticationSettingsHelper.ExtractException(asyncOp.Exception));
                Waiting?.Invoke(this, false);
                return;
            }

            // Simply trigger delete event to notify parent to remove the element from the list.
            Deleted?.Invoke(this);
            ResetCurrentValue();
            Waiting?.Invoke(this, false);
        }

        int DisplayDialogComplex(string title, string message, string ok, string cancel, string alt)
        {
            if (Application.isBatchMode || m_SkipConfirmation)
                return 0;

            return EditorUtility.DisplayDialogComplex(title, message, ok, cancel, alt);
        }
    }
}

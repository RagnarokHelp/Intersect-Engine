using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Utilities;

namespace Intersect.Client.Interface.Menu;


public partial class ForgotPasswordWindow
{

    private Button mBackBtn;

    private RichLabel mHintLabel;

    private Label mHintLabelTemplate;

    //Controls
    private ImagePanel mInputBackground;

    private Label mInputLabel;

    private TextBox mInputTextbox;

    //Parent
    private MainMenu mMainMenu;

    //Controls
    private ImagePanel mResetWindow;

    private Button mSubmitBtn;

    private Label mWindowHeader;

    //Init
    public ForgotPasswordWindow(Canvas parent, MainMenu mainMenu)
    {
        //Assign References
        mMainMenu = mainMenu;

        //Main Menu Window
        mResetWindow = new ImagePanel(parent, "ForgotPasswordWindow");
        mResetWindow.IsHidden = true;

        //Menu Header
        mWindowHeader = new Label(mResetWindow, "Header");
        mWindowHeader.SetText(Strings.ForgotPass.Title);

        mInputBackground = new ImagePanel(mResetWindow, "InputPanel");

        //Login Username Label
        mInputLabel = new Label(mInputBackground, "InputLabel");
        mInputLabel.SetText(Strings.ForgotPass.Label);

        //Login Username Textbox
        mInputTextbox = new TextBox(mInputBackground, "InputField");
        mInputTextbox.SubmitPressed += Textbox_SubmitPressed;
        mInputTextbox.Clicked += Textbox_Clicked;

        mHintLabelTemplate = new Label(mResetWindow, "HintLabel");
        mHintLabelTemplate.IsHidden = true;

        //Login - Send Login Button
        mSubmitBtn = new Button(mResetWindow, "SubmitButton");
        mSubmitBtn.SetText(Strings.ForgotPass.Submit);
        mSubmitBtn.Clicked += SubmitBtn_Clicked;

        //Login - Back Button
        mBackBtn = new Button(mResetWindow, "BackButton");
        mBackBtn.SetText(Strings.ForgotPass.Back);
        mBackBtn.Clicked += BackBtn_Clicked;

        mResetWindow.LoadJsonUi(GameContentManager.UI.Menu, Graphics.Renderer.GetResolutionString());

        mHintLabel = new RichLabel(mResetWindow);
        mHintLabel.SetBounds(mHintLabelTemplate.Bounds);
        mHintLabelTemplate.IsHidden = false;
        mHintLabel.AddText(Strings.ForgotPass.Hint, mHintLabelTemplate);
    }

    public bool IsHidden => mResetWindow.IsHidden;

    private void Textbox_Clicked(Base sender, MouseButtonState arguments)
    {
        Globals.InputManager.OpenKeyboard(KeyboardType.Normal, mInputTextbox.Text, false, false, false);
    }

    //Methods
    public void Update()
    {
        if (!Networking.Network.IsConnected)
        {
            Hide();
            mMainMenu.Show();
        }

        // Re-Enable our buttons if we're not waiting for the server anymore with it disabled.
        if (!Globals.WaitingOnServer && mSubmitBtn.IsDisabled)
        {
            mSubmitBtn.Enable();
        }
    }

    public void Hide()
    {
        mResetWindow.IsHidden = true;
    }

    public void Show()
    {
        mResetWindow.IsHidden = false;
        mInputTextbox.Text = string.Empty;
    }

    void BackBtn_Clicked(Base sender, MouseButtonState arguments)
    {
        Hide();
        Interface.MenuUi.MainMenu.NotifyOpenLogin();
    }

    void Textbox_SubmitPressed(Base sender, EventArgs arguments)
    {
        TrySendCode();
    }

    void SubmitBtn_Clicked(Base sender, MouseButtonState arguments)
    {
        if (Globals.WaitingOnServer)
        {
            return;
        }

        TrySendCode();

        mSubmitBtn.Disable();
    }

    public void TrySendCode()
    {
        if (!Networking.Network.IsConnected)
        {
            Interface.ShowAlert(Strings.Errors.NotConnected, alertType: AlertType.Error);

            return;
        }

        if (!FieldChecking.IsValidUsername(mInputTextbox?.Text, Strings.Regex.Username) &&
            !FieldChecking.IsWellformedEmailAddress(mInputTextbox?.Text, Strings.Regex.Email))
        {
            Interface.ShowAlert(Strings.Errors.UsernameInvalid, alertType: AlertType.Error);
            return;
        }

        Interface.MenuUi.MainMenu.OpenResetPassword(mInputTextbox?.Text);
        PacketSender.SendRequestPasswordReset(mInputTextbox?.Text);
    }

}

using Vintagestory.API.MathTools;

namespace claims.src.gui.playerGui
{
    /// <summary>
    /// All mutable UI state of the claims dialog. Lives outside CANClaimsGui so pages and dialog
    /// panels can stay stateless: they read and write here instead of holding fields of their own.
    /// </summary>
    public sealed class ClaimsGuiState
    {
        public EnumSelectedTab SelectedTab;
        public EnumSelectedTab ConflictSourceTab = EnumSelectedTab.AllianceInfoPage;
        public EnumUpperWindowSelectedState Dialog = EnumUpperWindowSelectedState.NONE;
        public DialogArgs DialogArgs = new DialogArgs();

        public int SelectedTabGroup;
        public int SelectedColor = -1;

        /// <summary>
        /// The coat of arms being edited. Held client-side until Apply - a stack of six layers is
        /// built one pick at a time.
        /// </summary>
        public EmblemEditState Emblem = new EmblemEditState();

        /// <summary>How the world-wide alliance list is ordered.</summary>
        public EnumAllianceSort AllianceSort = EnumAllianceSort.Default;
        public EnumCitySort CitySort = EnumCitySort.Default;

        /// <summary>What the admin pages are working on. Empty for everyone who is not an admin.</summary>
        public AdminPageState Admin = new AdminPageState();

        /// <summary>
        /// Closes the secondary window and drops whatever was typed into it.
        ///
        /// Selected and Pos survive: they say which rank, group or conflict the page behind the
        /// dialog is showing, not what the dialog was asking for. Clearing them left pages like the
        /// rank info page pointing at nothing, so reopening the dialog showed an empty tab.
        /// </summary>
        public void CloseDialog()
        {
            Dialog = EnumUpperWindowSelectedState.NONE;
            DialogArgs.Text = "";
            DialogArgs.First = "";
            DialogArgs.Second = "";
        }
    }

    /// <summary>
    /// The working copy of a coat of arms while the emblem page is open: which part it belongs to,
    /// the layers so far, and what the two pickers are pointing at.
    /// </summary>
    public sealed class EmblemEditState
    {
        /// <summary>Whether the alliance's emblem is being edited rather than the city's.</summary>
        public bool ForAlliance;

        public System.Collections.Generic.List<string> Layers = new System.Collections.Generic.List<string>();

        public string Pattern = "";
        public string Color = "";

        /// <summary>
        /// Starts an edit from what the part carries now, discarding any older draft. Sanitized on
        /// the way in, so an emblem stored before the background rule stays editable.
        /// </summary>
        public void Begin(bool forAlliance, string current)
        {
            ForAlliance = forAlliance;
            Layers = part.structure.EmblemHandler.Sanitize(part.structure.EmblemHandler.Parse(current));
            Pattern = "";
            Color = "";
        }

        public string AsString() => part.structure.EmblemHandler.Join(Layers);
    }

    /// <summary>
    /// What the admin pages currently point at. Separate from DialogArgs because it has to survive
    /// dialogs opening and closing on top of it.
    /// </summary>
    public sealed class AdminPageState
    {
        /// <summary>City the cities page is acting on.</summary>
        public string SelectedCity = "";
        public string CityFilter = "";
        public string NewCityName = "";
        public string RenameTo = "";
        public string PlayerName = "";
        public string BonusClaims = "0";
        public string CityFee = "0";
        public string ClaimRadius = "5";
        /// <summary>Deleting a city asks twice; this is the first answer.</summary>
        public bool ConfirmDelete;

        /// <summary>Player the player page is acting on.</summary>
        public string SelectedPlayer = "";

        /// <summary>Narrows the war settings list, which is some fifty rows long.</summary>
        public string WarCfgFilter = "";

        /// <summary>
        /// How far the war settings list is scrolled. Kept here because acting on a row rebuilds the
        /// window, and a fresh scrollbar starts at the top - which threw the admin back to the first
        /// row after every click.
        /// </summary>
        public float WarCfgScroll;
    }

    /// <summary>
    /// Arguments handed to the secondary window when it opens. What each field means is decided by
    /// whoever opens the dialog - previously a single buffer named collectedNewCityName carried a
    /// city name, a player name, a plot price, a tax rate and a rank name depending on the caller.
    /// </summary>
    public sealed class DialogArgs
    {
        /// <summary>The text input buffer, and any value pre-filled into it.</summary>
        public string Text = "";
        public string First = "";
        public string Second = "";
        /// <summary>The item the dialog acts on - a rank name, a plots group guid, a conflict guid.</summary>
        public string Selected = "";
        public string SelectedSecond = "";
        /// <summary>Block position the dialog acts on - a prison cell, a summon point.</summary>
        public Vec3i Pos;
    }
}

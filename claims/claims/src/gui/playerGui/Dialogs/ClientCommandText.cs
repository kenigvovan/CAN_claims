namespace claims.src.gui.playerGui.Dialogs
{
    /// <summary>
    /// Builds the command a form sends. string.Format turned a missing value - a plots group deleted
    /// while its form was open, a dropdown nobody picked from - into a double space, and the server
    /// answered with "Argument 2 is missing", which read as if the player had typed it wrong.
    /// </summary>
    public static class ClientCommandText
    {
        /// <summary>
        /// The command followed by its arguments, or null when any argument is missing. Null is
        /// what <see cref="CANGuiDialogPanel"/>'s Send refuses to send.
        /// </summary>
        public static string Join(string command, params string[] args)
        {
            if (args != null)
            {
                foreach (string arg in args)
                {
                    if (string.IsNullOrWhiteSpace(arg)) return null;
                }
            }
            return args == null || args.Length == 0 ? command : command + " " + string.Join(" ", args);
        }
    }
}

using System.Collections.Generic;
using System.Text;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     検証で見つかった不備と補足をまとめる。
    /// </summary>
    internal sealed class BuildValidationReport
    {
        #region 外部向けAPI

        /// <summary>
        ///     検証名を指定して結果を作成する。
        /// </summary>
        internal BuildValidationReport(string validationName)
        {
            ValidationName = validationName;
        }

        /// <summary> 検証名。 </summary>
        internal string ValidationName { get; }

        /// <summary> 検出した不備。 </summary>
        internal IReadOnlyList<string> Errors => _errors.AsReadOnly();

        /// <summary> 不備があるか。 </summary>
        internal bool HasErrors => _errors.Count != 0;

        /// <summary>
        ///     対象が分かるエラーを追加する。
        /// </summary>
        internal void AddError(string path, string message)
        {
            _errors.Add($"{message} Path: {path}");
        }

        /// <summary>
        ///     検査を省略した理由などの補足を追加する。
        /// </summary>
        internal void AddNote(string message)
        {
            _notes.Add(message);
        }

        /// <summary>
        ///     ダイアログとログに共通の結果文を作る。
        /// </summary>
        internal string CreateMessage()
        {
            StringBuilder message = new();
            message.Append('[').Append(ValidationName).Append("] ");
            message.Append(HasErrors ? $"{_errors.Count}件の不備があります。" : "検証エラーはありません。");
            foreach (string error in _errors) { message.Append("\n- ").Append(error); }
            foreach (string note in _notes) { message.Append("\n").Append(note); }
            return message.ToString();
        }

        #endregion

        #region 内部処理

        private readonly List<string> _errors = new();
        private readonly List<string> _notes = new();

        #endregion
    }
}

/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.FileEdit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Managers;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Dialog that allows selecting a file on the hard drive
    /// </summary>
    public class SelectFileDialog : MonoBehaviour
    {
        /// <summary>
        /// Callback with the selected file path that will be invoked after selecting a file
        /// </summary>
        private Action<string> callback;
        
        /// <summary>
        /// File extensions that will be visible in the dialog, if null all extensions are viewed
        /// </summary>
        private string[] viewedExtensions;
        
        /// <summary>
        /// Currently selected file button
        /// </summary>
        private SelectFileDialogFileButton selectedFile;
        
        /// <summary>
        /// All of the currently available file buttons in the current directory
        /// </summary>
        private List<SelectFileDialogFileButton> filesButtons = new List<SelectFileDialogFileButton>();
        
        /// <summary>
        /// Is dialog allowing custom filename input, should be false for load commands
        /// </summary>
        private bool allowCustomFilename;
        
        /// <summary>
        /// Currently viewed directory path
        /// </summary>
        private string currentPath;

        /// <summary>
        /// Dialog title text
        /// </summary>
        public Text title;
        
        /// <summary>
        /// Input field for the manual directory path selection
        /// </summary>
        public InputField directoryPathInputField;
        
        /// <summary>
        /// Grid where all the buttons for current files will be added
        /// </summary>
        public RectTransform filesGrid;
        
        /// <summary>
        /// Sample of the file button used in this dialog
        /// </summary>
        public SelectFileDialogFileButton fileButtonSample;
        
        /// <summary>
        /// Input field for the manual file name selection
        /// </summary>
        public InputField customFileNameInputField;

        /// <summary>
        /// Text of the select file button
        /// </summary>
        public Text selectFileText;

        /// <summary>
        /// Currently viewed directory path
        /// </summary>
        public string DirectoryPath => currentPath;
        
        /// <summary>
        /// Checks if the dialog can be showed
        /// </summary>
        public bool CanBeShown => !ScenarioManager.Instance.ViewsPopup;

        /// <summary>
        /// Global path of the currently selected file 
        /// </summary>
        public string FilePath => allowCustomFilename
            ? $"{DirectoryPath}{customFileNameInputField.text}"
            : $"{DirectoryPath}{selectedFile.title.text}";

        /// <summary>
        /// Shows the file select dialog with parametrised settings
        /// </summary>
        /// <param name="pathSelected">Callback that will be invoked after path is selected</param>
        /// <param name="allowCustomFilename">Is dialog allowing custom filename input, should be false for load commands</param>
        /// <param name="directoryPath">Starting directory path</param>
        /// <param name="dialogTitle">Title text of the dialog</param>
        /// <param name="selectFileTitle">Text of the select file button</param>
        /// <param name="extensions">File extensions that will be visible in the dialog, if null all extensions are viewed</param>
        public void Show(Action<string> pathSelected, bool allowCustomFilename, string directoryPath = null,
            string dialogTitle = "Select File Dialog", string selectFileTitle = "Select File", string[] extensions = null)
        {
            if (!CanBeShown)
                return;
            callback = pathSelected;
            this.allowCustomFilename = allowCustomFilename;
            customFileNameInputField.text = "";
            title.text = dialogTitle;
            selectFileText.text = selectFileTitle;
            viewedExtensions = extensions;
            var path = directoryPath ?? Application.persistentDataPath;
            SelectDirectoryPath(path);
            customFileNameInputField.interactable = allowCustomFilename;
                
            ScenarioManager.Instance.ViewsPopup = true;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the dialog, does not invoke the callback
        /// </summary>
        public void Hide()
        {
            ClearFilesGrid();
            ScenarioManager.Instance.ViewsPopup = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Selects new directory path
        /// </summary>
        /// <param name="path">Path of the directory to be selected</param>
        public void SelectDirectoryPath(string path)
        {
            if (path == currentPath)
                return;
            if (!Directory.Exists(path))
            {
                path = Path.GetDirectoryName(path);
                if (!Directory.Exists(path))
                    return;
            }

            path = PathAddDirectorySeparator(path);
            directoryPathInputField.text = path;
            currentPath = path;

            ClearFilesGrid();

            var directories = Directory.GetDirectories(path);
            foreach (var directory in directories)
            {
                var buttonGameObject = ScenarioManager.Instance.prefabsPools.GetInstance(fileButtonSample.gameObject);
                buttonGameObject.transform.SetParent(filesGrid);
                buttonGameObject.SetActive(true);
                var button = buttonGameObject.GetComponent<SelectFileDialogFileButton>();
                button.ParentDialog = this;
                button.MarkAsDirectory(Path.GetFileName(directory));
                filesButtons.Add(button);
            }

            var searchPattern = "*";
            if (viewedExtensions != null)
            {
                var sb = new StringBuilder();
                for (var i = 0; i < viewedExtensions.Length; i++)
                {
                    var viewedExtension = viewedExtensions[i];
                    sb.Append("*.");
                    sb.Append(viewedExtension);
                    if (i < viewedExtensions.Length - 1)
                        sb.Append("|");
                }

                searchPattern = sb.ToString();
            }

            var files = Directory.GetFiles(path, searchPattern);
            foreach (var file in files)
            {
                var buttonGameObject = ScenarioManager.Instance.prefabsPools.GetInstance(fileButtonSample.gameObject);
                buttonGameObject.transform.SetParent(filesGrid);
                buttonGameObject.SetActive(true);
                var button = buttonGameObject.GetComponent<SelectFileDialogFileButton>();
                button.ParentDialog = this;
                button.MarkAsFile(Path.GetFileName(file));
                filesButtons.Add(button);
            }
        }

        /// <summary>
        /// Clears whole grid of the file buttons
        /// </summary>
        private void ClearFilesGrid()
        {
            //Clear files grid
            if (selectedFile != null)
                selectedFile.Unselect();
            selectedFile = null;
            for (var i = filesButtons.Count - 1; i >= 0; i--)
            {
                var fileButton = filesButtons[i];
                ScenarioManager.Instance.prefabsPools.ReturnInstance(fileButton.gameObject);
            }

            filesButtons.Clear();
        }

        /// <summary>
        /// Views the parent directory of the currently viewed directory.
        /// </summary>
        public void MoveToUpDirectory()
        {
            var separators = new[] {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar};
            var directoryPath = Path.GetDirectoryName(DirectoryPath.TrimEnd(separators));
            if (!string.IsNullOrEmpty(directoryPath))
                SelectDirectoryPath(directoryPath);
        }

        /// <summary>
        /// Apply the file, invokes callback with currently selected file path and closes the dialog
        /// </summary>
        public void ApplyFile()
        {
            if (allowCustomFilename ? !string.IsNullOrEmpty(customFileNameInputField.text) : selectedFile != null)
            {
                callback?.Invoke(FilePath);
                Hide();
            }
        }

        /// <summary>
        /// Selects the file button, does not finish the file selecting, use ApplyFile to finish selecting file
        /// </summary>
        /// <param name="button">Selected file button</param>
        public void SelectFile(SelectFileDialogFileButton button)
        {
            UnselectFile();
            selectedFile = button;
            selectedFile.Select();
            customFileNameInputField.SetTextWithoutNotify(selectedFile.title.text);
        }

        /// <summary>
        /// Unselects current file button
        /// </summary>
        public void UnselectFile()
        {
            if (selectedFile == null) return;
            selectedFile.Unselect();
            selectedFile = null;
        }

        /// <summary>
        /// Adds the directory separator to the path proper for the operating system
        /// </summary>
        /// <param name="path">Path where the directory separator will be added</param>
        /// <returns>Path with added directory separator proper for the operating system</returns>
        private string PathAddDirectorySeparator(string path)
        {
            // They're always one character but EndsWith is shorter than
            // array style access to last path character. Change this
            // if performance are a (measured) issue.
            string separator1 = Path.DirectorySeparatorChar.ToString();
            string separator2 = Path.AltDirectorySeparatorChar.ToString();

            // Trailing white spaces are always ignored but folders may have
            // leading spaces. It's unusual but it may happen. If it's an issue
            // then just replace TrimEnd() with Trim(). Tnx Paul Groke to point this out.
            path = path.TrimEnd();

            // Argument is always a directory name then if there is one
            // of allowed separators then I have nothing to do.
            if (path.EndsWith(separator1) || path.EndsWith(separator2))
                return path;

            // If there is the "alt" separator then I add a trailing one.
            // Note that URI format (file://drive:\path\filename.ext) is
            // not supported in most .NET I/O functions then we don't support it
            // here too. If you have to then simply revert this check:
            // if (path.Contains(separator1))
            //     return path + separator1;
            //
            // return path + separator2;
            if (path.Contains(separator2))
                return path + separator2;

            // If there is not an "alt" separator I add a "normal" one.
            // It means path may be with normal one or it has not any separator
            // (for example if it's just a directory name). In this case I
            // default to normal as users expect.
            return path + separator1;
        }
    }
}
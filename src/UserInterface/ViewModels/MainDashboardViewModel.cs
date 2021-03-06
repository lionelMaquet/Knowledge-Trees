﻿using Caliburn.Micro;
using Services.Constants;
using Services.Interfaces;
using System.Collections.ObjectModel;
using System.IO.Abstractions;
using System.Windows;
using System.Windows.Input;
using UserInterface.ViewModels.Commands.MainDashboard;

namespace UserInterface.ViewModels
{
    public class MainDashboardViewModel : Conductor<object>
    {
        #region Fields & properties

        private readonly IWindowManager _windowManager;
        private readonly IFolderLogicHandler _folderLogicHandler;
        private readonly IWordLogicHandler _wordLogicHandler;
        private readonly IFileSystem _fileSystem;

        public CreateLeafViewModel _createLeafViewModel;
        public CreateTreeViewModel CreateTreeViewModel { get; set; }
        private ViewTreeViewModel _viewTreeViewModel;

        private ObservableCollection<string> _trees;
        public ObservableCollection<string> Trees
        {
            get { return _trees; }
            set
            {
                _trees = value;
                NotifyOfPropertyChange(() => Trees);
            }
        }

        private ObservableCollection<string> _leaves;
        public ObservableCollection<string> Leaves
        {
            get { return _leaves; }
            set
            {
                _leaves = value;
                NotifyOfPropertyChange(() => Leaves);
            }
        }

        internal string DefaultLeavesHeaderTitle = "Leaves list";

        private string _selectedTree;
        public string SelectedTree
        {
            get { return _selectedTree ?? DefaultLeavesHeaderTitle; }
            set
            {
                _selectedTree = value;
                NotifyOfPropertyChange(() => SelectedTree);
                UpdateLeavesList();
                UpdateViewAndDeleteButtonStatus();
            }
        }

        private string _selectedLeaf;
        public string SelectedLeaf
        {
            get { return _selectedLeaf; }
            set
            {
                _selectedLeaf = value;
                NotifyOfPropertyChange(() => SelectedLeaf);
                UpdateAddAndRemoveButtonStatus();
            }
        }

        private bool _createLeafButtonEnabled;
        public bool CreateLeafButtonEnabled
        {
            get { return _createLeafButtonEnabled; }
            set
            {
                _createLeafButtonEnabled = value;
                NotifyOfPropertyChange(() => CreateLeafButtonEnabled);
            }
        }

        private bool _addAndRemoveLeafButtonsEnabled;
        public bool AddAndRemoveLeafButtonsEnabled
        {
            get { return _addAndRemoveLeafButtonsEnabled; }
            set
            {
                _addAndRemoveLeafButtonsEnabled = value;
                NotifyOfPropertyChange(() => AddAndRemoveLeafButtonsEnabled);
            }
        }

        private bool _viewAndRemoveTreeButtonsEnabled;
        public bool ViewAndRemoveTreeButtonsEnabled
        {
            get { return _viewAndRemoveTreeButtonsEnabled; }
            set
            {
                _viewAndRemoveTreeButtonsEnabled = value;
                NotifyOfPropertyChange(() => ViewAndRemoveTreeButtonsEnabled);
            }
        }

        public ICommand DeleteTreeCommand { get; set; }
        public ICommand ViewTreeCommand { get; set; }
        public ICommand DeleteLeafCommand { get; set; }
        public ICommand ViewLeafCommand { get; set; }

        #endregion

        #region Constructors

        public MainDashboardViewModel(IWindowManager windowManager, IFolderLogicHandler folderLogicHandler, 
            IWordLogicHandler wordLogicHandler, IFileSystem fileSystem)
        {
            _windowManager = windowManager;
            _folderLogicHandler = folderLogicHandler;
            _wordLogicHandler = wordLogicHandler;
            _fileSystem = fileSystem;

            DeleteTreeCommand = new DeleteTreeCommand(this);
            ViewTreeCommand = new ViewTreeCommand(this);

            DeleteLeafCommand = new DeleteLeafCommand(this);
            ViewLeafCommand = new ViewLeafCommand(this);

            UpdateTreesList();
            UpdateLeavesList();
        }

        #endregion

        #region Methods to call new ViewModels

        public void OpenCreateTreeWindow()
        {
            if (CreateTreeViewModel == null)
            {
                CreateTreeViewModel = new CreateTreeViewModel(_folderLogicHandler, this);
            }

            CreateTreeViewModel.TryClose();
            _windowManager.ShowWindow(CreateTreeViewModel);
        }

        public void OpenCreateLeafWindow()
        {
            if (_createLeafViewModel == null)
            {
                _createLeafViewModel = 
                    new CreateLeafViewModel(_wordLogicHandler, this, SelectedTree);
            }

            _createLeafViewModel.TryClose();
            _windowManager.ShowWindow(_createLeafViewModel);
        }

        public void OpenViewTreeWindow()
        {
            if (_viewTreeViewModel == null)
            {
                _viewTreeViewModel = new ViewTreeViewModel(_wordLogicHandler, SelectedTree);
            }

            _viewTreeViewModel.TreeName = SelectedTree;
            _viewTreeViewModel.StatisticsReportMessage = "";
            _viewTreeViewModel.TryClose();
            _windowManager.ShowWindow(_viewTreeViewModel);
        }

        #endregion

        #region Methods to update dashboard data and buttons status.

        public void UpdateTreesList()
        {
            Trees = new ObservableCollection<string>();

            var allTreeNames = _folderLogicHandler.GetAllTreeNames(DirectoryConstants.CurrentWorkingPath);

            foreach (var treeName in allTreeNames)
                Trees.Add(treeName);
        }

        public void UpdateLeavesList()
        {
            Leaves = new ObservableCollection<string>();

            if (SelectedTree != null && SelectedTree != DefaultLeavesHeaderTitle)
            {
                var treePath = DirectoryConstants.CurrentWorkingPath + $@"\{SelectedTree}";
                var leaves = _folderLogicHandler.GetAllLeafNamesWithNoExtension(treePath);

                foreach (var leaf in leaves)
                    Leaves.Add(leaf);

                CreateLeafButtonEnabled = true;
            }
            else
            {
                Leaves = null;
                CreateLeafButtonEnabled = false;
            }
        }

        public void UpdateAddAndRemoveButtonStatus()
        {
            if (SelectedLeaf != null)
                AddAndRemoveLeafButtonsEnabled = true;
            else
                AddAndRemoveLeafButtonsEnabled = false;
        }

        private void UpdateViewAndDeleteButtonStatus()
        {
            if (SelectedTree == null)
                ViewAndRemoveTreeButtonsEnabled = false;
            else
                ViewAndRemoveTreeButtonsEnabled = true;
        }

        #endregion

        #region Other methods

        public void DeleteTree()
        {
            bool canDelete = Trees.Count >= 1 && SelectedTree != null; 

            if (canDelete)
            {
                var dialogueResult = MessageBox.Show("Are you sure you want to delete this tree? " +
                "All leaves will be deleted. This cannot be undone.", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (dialogueResult == MessageBoxResult.Yes)
                {
                    var treePath = DirectoryConstants.CurrentWorkingPath + $@"\{SelectedTree}";

                    _wordLogicHandler.SaveAndCloseAllLeaves();
                    _folderLogicHandler.DeleteTree(treePath);

                    UpdateTreesList();
                }
            }
            else
            {
                MessageBox.Show("Please select a tree to delete first.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        public void DeleteLeaf()
        {
            bool canDelete = Leaves.Count >= 1 && SelectedLeaf != null;

            if (canDelete)
            {
                var dialogueResult = MessageBox.Show("Are you sure you want to delete this leaf? " +
                "This cannot be undone.", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (dialogueResult == MessageBoxResult.Yes)
                {
                    var leafPath = _folderLogicHandler.GetFullLeafPath(SelectedTree, SelectedLeaf);
                    bool leafIsOpen = _wordLogicHandler.CheckIfLeafIsOpen(leafPath);

                    if (leafIsOpen)
                    {
                        MessageBox.Show("Please close the leaf before deleting it.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }

                    _folderLogicHandler.DeleteLeaf(leafPath);
                    UpdateLeavesList();
                }
            }
            else
            {
                MessageBox.Show("Please select a leaf to delete first.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        public void ViewLeaf()
        {
            string path = _folderLogicHandler.GetFullLeafPath(SelectedTree, SelectedLeaf);

            _wordLogicHandler.OpenExistingLeaf(path);
        }

        #endregion
    }
}

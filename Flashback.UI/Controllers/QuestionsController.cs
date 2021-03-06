using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoTouch.UIKit;
using Flashback.Core;
using MonoTouch.Foundation;

namespace Flashback.UI.Controllers
{
	/// <summary>
	/// Displays the questions as a table and enables moving and deleting.
	/// </summary>
	public class QuestionsController : UITableViewController
	{
		private UIBarButtonItem _addButton;
		private UIBarButtonItem _editButton;
		private UIBarButtonItem _doneButton;

		private QuestionsData _data;
		private Category _category;
		private AddEditQuestionController _addEditQuestionController;
		private QuestionsTableSource _questionsTableSource;

		/// <summary>
		/// Creates a new instance of <see cref="QuestionsController"/>
		/// </summary>
		/// <param name="category"></param>
		public QuestionsController(Category category) : base(UITableViewStyle.Grouped)
		{
			_data = new QuestionsData(category);
			_category = category;
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			
			Title = _category.Name;
			ToolbarItems = GetToolBar();
			NavigationController.ToolbarHidden = false;

			// Edit and done button
			_editButton = new UIBarButtonItem(UIBarButtonSystemItem.Edit);
			_editButton.Clicked += delegate(object sender, EventArgs e)
			{
				TableView.Editing = true;
				NavigationItem.SetRightBarButtonItem(_doneButton, false);
			};

			_doneButton = new UIBarButtonItem(UIBarButtonSystemItem.Done);
			_doneButton.Clicked += DoneClick;

			NavigationItem.SetRightBarButtonItem(_editButton, false);

			// Setup the datasource
			ReloadData();
		}
		
		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear(animated);
			NavigationController.ToolbarHidden = false;
		}
		
		/// <summary>
		/// Recreates the datasource and binds it to the table.
		/// </summary>
		public void ReloadData()
		{
			_data = new QuestionsData(_category);
			_questionsTableSource = new QuestionsTableSource(_data, this);
			TableView.Source = _questionsTableSource;
			TableView.ReloadData();
		}
		
		private void DoneClick(object sender, EventArgs e)
		{
			TableView.Editing = false;
			NavigationItem.SetRightBarButtonItem(_editButton, false);
			
			// Save the order
			for (int i = 0; i < _data.Questions.Count; i++)
			{
				_data.Questions[i].Order = i;
				Question.Save(_data.Questions[i]);
			}	
		}

		private UIBarButtonItem[] GetToolBar()
		{
			// Add button
			_addButton = new UIBarButtonItem();
			_addButton.Image = UIImage.FromFile("Assets/Images/Toolbar/toolbar_add.png");
			_addButton.Title = "Add question";
			_addButton.Clicked += delegate
			{
				if (Settings.IsFullVersion)
				{
					_addEditQuestionController = new AddEditQuestionController(null, _data.Category);
					NavigationController.PushViewController(_addEditQuestionController, true);
				}
				else
				{
					if (_data.Questions.Count == 10)
					{
						UpgradeView view = new UpgradeView();
						view.Show("The free edition is limited to 10 questions.");
					}
					else
					{
						_addEditQuestionController = new AddEditQuestionController(null, _data.Category);
						NavigationController.PushViewController(_addEditQuestionController, true);
					}
				}
			};

			return new UIBarButtonItem[] { _addButton };
		}

		#region QuestionsTableViewSource
		private class QuestionsTableSource : UITableViewSource
		{
			private QuestionsData _data;
			private QuestionsController _questionsController;
			private AddEditQuestionController _addEditQuestionsController;

			public QuestionsTableSource(QuestionsData data, QuestionsController questionsController)
			{
				_data = data;
				_questionsController = questionsController;
			}

			public override int RowsInSection(UITableView tableview, int section)
			{
				return _data.Questions.Count;
			}

			public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
			{
				UITableViewCell cell = tableView.DequeueReusableCell("cellid");

				if (cell == null)
				{
					cell = new UITableViewCell(UITableViewCellStyle.Subtitle, "cellid");
					cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
				}

				cell.TextLabel.Text = _data.Questions[indexPath.Row].Title;
				cell.DetailTextLabel.Text = _data.Questions[indexPath.Row].Answer;

				return cell;
			}

			public override void CommitEditingStyle(UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath)
			{
				if (editingStyle == UITableViewCellEditingStyle.Delete)
				{
					Question question = _data.Questions[indexPath.Row];
					Question.Delete(question.Id);
					_data.DeleteRow(question);

					tableView.DeleteRows(new[] { indexPath }, UITableViewRowAnimation.Fade);
				}
			}

			public override void MoveRow(UITableView tableView, NSIndexPath sourceIndexPath, NSIndexPath destinationIndexPath)
			{
				// Swap the two array items around
				_data.MoveQuestion(sourceIndexPath.Row,destinationIndexPath.Row);
			}

			public override bool CanMoveRow(UITableView tableView, NSIndexPath indexPath)
			{
				return true;
			}

			public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
			{
				Question question = _data.Questions[indexPath.Row];
				_addEditQuestionsController = new AddEditQuestionController(question,question.Category);
				_questionsController.NavigationController.PushViewController(_addEditQuestionsController, true);
			}
		}
		#endregion

		/// <summary>
		/// Holds the categories, and questions sorted by order, to avoid roundtrips to the database.
		/// </summary>
		public class QuestionsData
		{
			public List<Question> Questions { get; set; }
			public Category Category { get; set;}

			public QuestionsData(Category category)
			{
				Category = category;
				Questions = Question.ForCategory(category).OrderBy(q => q.Order).ToList();
			}
			
			public void DeleteRow(Question question)
			{
				Questions.Remove(question);	
			}
			
			/// <summary>
			/// Moves the source question to the destination, removing it first.
			/// </summary>
			public void MoveQuestion(int sourceIndex,int destIndex)
			{
				Question question = Questions[sourceIndex];			
				Questions.RemoveAt(sourceIndex);
				Questions.Insert(destIndex,question);
				
				Logger.Info("Moved {0}[{1}] to {2}",question,sourceIndex,destIndex);
			}
		}
	}
}

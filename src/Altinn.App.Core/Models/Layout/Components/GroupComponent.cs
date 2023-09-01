using System.Text.Json;

using Altinn.App.Core.Models.Expressions;

namespace Altinn.App.Core.Models.Layout.Components;

/// <summary>
/// Tag component to signify that this is a group component
/// </summary>
public class GroupComponent : BaseComponent
{
    /// <summary>
    /// Constructor for GroupComponent
    /// </summary>
    public GroupComponent(string id, string type, IReadOnlyDictionary<string, string>? dataModelBindings, IEnumerable<BaseComponent> children, IEnumerable<string>? childIDs, Expression? hidden, Expression? required, Expression? readOnly, IReadOnlyDictionary<string, string>? additionalProperties) :
        base(id, type, dataModelBindings, hidden, required, readOnly, additionalProperties)
    {

        Children = children;
        ChildIDs = childIDs ?? children.Select(c => c.Id);
        foreach (var child in Children)
        {
            child.Parent = this;
        }
    }

    /// <summary>
    /// The children in this group/page
    /// </summary>
    public IEnumerable<BaseComponent> Children { get; private set; }

    /// <summary>
    /// The child IDs in this group/page
    /// </summary>
    public IEnumerable<string> ChildIDs { get; private set; }

    /// <summary>
    /// Adds a child component which is already defined in its child IDs
    /// </summary>
    public virtual void AddChild(BaseComponent child)
    {
        if (!this.ChildIDs.Contains(child.Id))
        {
            throw new ArgumentException($"Attempted to add child with id {child.Id} to group {this.Id}, but this child is not included in its list of child IDs");
        }
        if (this.Children.FirstOrDefault(c => c.Id == child.Id) != null)
        {
            throw new ArgumentException($"Attempted to add child with id {child.Id} to group {this.Id}, but a child with this id has already been added");
        }
        child.Parent = this;
        this.Children = this.Children.Append(child);
    }

    /// <summary>
    /// Validates that the children in this group matches the child IDs and orders the children according to the child IDs
    /// </summary>
    public void ValidateChildren()
    {
        var childIDs = this.ChildIDs.ToList();

        foreach (var childID in childIDs)
        {
            if (!this.Children.Select(c => c.Id).Contains(childID))
            {
                throw new ArgumentException($"Child with id {childID} could not be found for the group {this.Id}");
            }
        }

        var childIDCount = childIDs.Count;
        var childCount = this.Children.Count();

        if (childCount != childIDCount)
        {
            throw new ArgumentException($"The number of children ({childCount}) in group {this.Id} does not match the number of child IDs provided ({childIDCount})");
        }

        this.Children = this.Children.OrderBy(c => childIDs.IndexOf(c.Id));

        foreach (var child in this.Children)
        {
            if (child is GroupComponent group)
            {
                group.ValidateChildren();
            }
        }
    }
}

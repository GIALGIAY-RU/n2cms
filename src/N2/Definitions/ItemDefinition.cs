#region License

/* Copyright (C) 2007 Cristian Libardo
 *
 * This is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as
 * published by the Free Software Foundation; either version 2.1 of
 * the License, or (at your option) any later version.
 *
 * This software is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this software; if not, write to the Free
 * Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA
 * 02110-1301 USA, or see the FSF site: http://www.fsf.org.
 */

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;
using N2.Details;
using N2.Integrity;
using N2.Web.UI;

namespace N2.Definitions
{
	/// <summary>
	/// Represents the definition of a content item. Expose reflected 
	/// information a types attributes.
	/// </summary>
	public class ItemDefinition : IComparable<ItemDefinition>
	{
		#region Private Fields

		private readonly Type itemType;
		private DefinitionAttribute definitionAttribute;
		private string iconUrl = null;
		private IList<AvailableZoneAttribute> availableZones = new List<AvailableZoneAttribute>();
		private IList<string> allowedZoneNames = new List<string>();
		private IList<ItemDefinition> allowedChildren = new List<ItemDefinition>();
		private IList<IEditable> editables;
		private IList<IEditableContainer> containers;
		private IList<EditorModifierAttribute> modifiers;
		private IList<IDisplayable> displayables;
		private IList<string> authorizedRoles;
		private IEditableContainer rootContainer = null;
		private bool enabled = true;
		private bool isDefined = false;

		#endregion

		#region Constructor & Initialize

		/// <summary>Creates a new a instance of the ItemDefinition class loading the supplied type.</summary>
		/// <param name="itemType">The item type to define.</param>
		public ItemDefinition(Type itemType)
		{
			if (!itemType.IsSubclassOf(typeof(ContentItem))) throw new N2Exception("Can only create definitions of content items. This type is not a subclass of N2.ContentItem: " + itemType.FullName);

			this.itemType = itemType;
			definitionAttribute = new DefinitionAttribute(itemType.Name, itemType.Name, string.Empty, itemType.FullName, 1000);
		}

		#endregion

		#region Properties

		/// <summary>Gets or sets the item attribute defined for the item class.</summary>
		public DefinitionAttribute DefinitionAttribute
		{
			get { return definitionAttribute; }
			internal set { definitionAttribute = value; }
		}

		/// <summary>Gets roles or users allowed to edit items defined by this definition.</summary>
		public IList<string> AuthorizedRoles
		{
			get { return authorizedRoles; }
			internal set { authorizedRoles = value; }
		}

		/// <summary>Gets the name used when presenting this item class to editors.</summary>
		public string Title
		{
			get { return definitionAttribute.Title; }
		}

		/// <summary>Gets discriminator value used to to map class when retrieving from persistence. When this is null the type's full name is used.</summary>
		public string Discriminator
		{
			get { return definitionAttribute.Name ?? ItemType.Name; }
		}

		/// <summary>Definitions which are not enabled are not available when creating new items.</summary>
		public bool Enabled
		{
			get { return enabled; }
			set { enabled = value; }
		}

		/// <summary>Gets or sets wheter this definition has been defined. Weirdly enough a definition may exist without beeing defined. To define a definition the class must implement the <see cref="N2.DefinitionAttribute"/> attribute.</summary>
		public bool IsDefined
		{
			get { return isDefined; }
			internal set { isDefined = value; }
		}

		/// <summary>Gets the order of this item type when selecting new item in edit mode.</summary>
		public int SortOrder
		{
			get { return definitionAttribute.SortOrder; }
		}

		/// <summary>Gets the tooltip used when presenting this item class to editors.</summary>
		public string ToolTip
		{
			get { return definitionAttribute.ToolTip; }
		}

		/// <summary>Gets the description used when presenting this item class to editors.</summary>
		public string Description
		{
			get { return definitionAttribute.Description; }
		}

		/// <summary>Gets or sets the type of this item.</summary>
		public Type ItemType
		{
			get { return itemType; }
		}

		/// <summary>Gets zones available in this items of this class.</summary>
		public IList<AvailableZoneAttribute> AvailableZones
		{
			get { return availableZones; }
		}

		/// <summary>Gets zones this class of items can be placed in.</summary>
		public IList<string> AllowedZoneNames
		{
			get { return allowedZoneNames; }
			internal set { allowedZoneNames = value; }
		}

		/// <summary>Gets the IconUrl returned by a new instance of the item.</summary>
		public string IconUrl
		{
			get
			{
				if (iconUrl == null)
				{
					try
					{
						iconUrl = ((ContentItem)Activator.CreateInstance(ItemType)).IconUrl;
					}
					catch (Exception ex)
					{
						Trace.TraceWarning(ex.ToString());
						iconUrl = "";
					}
				}
				return iconUrl;
			}
		}

		/// <summary>Gets or sets editables defined for the item.</summary>
		public IList<IEditable> Editables
		{
			get { return editables; }
			set { editables = value; }
		}

		/// <summary>Gets or sets containers defined for the item.</summary>
		public IList<IEditableContainer> Containers
		{
			get { return containers; }
			set { containers = value; }
		}

		/// <summary>Gets or sets the root container used to build the edit interface.</summary>
		public IEditableContainer RootContainer
		{
			get { return rootContainer; }
			set { rootContainer = value; }
		}

		/// <summary>Gets or sets additional child types allowed below this item.</summary>
		public IList<ItemDefinition> AllowedChildren
		{
			get { return allowedChildren; }
		}

		/// <summary>Gets or sets all editor modifier attributes for this item.</summary>
		public IList<EditorModifierAttribute> Modifiers
		{
			get { return modifiers; }
			set { modifiers = value; }
		}

		/// <summary>Gets or sets displayable attributes defined for the item.</summary>
		public IList<IDisplayable> Displayables
		{
			get { return displayables; }
			set { displayables = value; }
		}

		#endregion

		#region Methods

		/// <summary>Find out if this item is allowed in a zone.</summary>
		/// <param name="zone"></param>
		/// <returns></returns>
		[Obsolete("Name changed to IsAllowedInZone.")]
		public bool IsZoneAllowed(string zone)
		{
			return IsAllowedInZone(zone);
		}

		/// <summary>Find out if this item is allowed in a zone.</summary>
		/// <param name="zone">The zone name to check.</param>
		/// <returns>True if the item is allowed in the zone.</returns>
		public bool IsAllowedInZone(string zone)
		{
			if(allowedZoneNames == null)
				return true;
			else
				return allowedZoneNames.Contains(zone);
		}

		/// <summary>Gets editable attributes available to user.</summary>
		/// <returns>A filtered list of editable fields.</returns>
		public IList<IEditable> GetEditables(IPrincipal user)
		{
			List<IEditable> filteredList = new List<IEditable>();
			foreach (IEditable e in Editables)
				if (e.IsAuthorized(user))
					filteredList.Add(e);
			return filteredList;
		}

		/// <summary>Gets the editor modifications for the specified detail name.</summary>
		/// <param name="detailName"></param>
		/// <returns></returns>
		public IList<EditorModifierAttribute> GetModifiers(string detailName)
		{
			List<EditorModifierAttribute> filtered = new List<EditorModifierAttribute>();
			foreach (EditorModifierAttribute a in Modifiers)
				if (a.Name == detailName)
					filtered.Add(a);
			return filtered;
		}

		/// <summary>Instantiates a new object of the defined content item class.</summary>
		/// <returns>A new instance of the defined content item type.</returns>
		[Obsolete("Use N2.Factory.Definitions.CreateInstance instead.")]
		public ContentItem CreateInstance(ContentItem parent)
		{
			ContentItem item = (ContentItem) Activator.CreateInstance(ItemType);

			item.Parent = parent;
			return item;
		}

		#endregion

		public bool HasZone(string zone)
		{
			if (string.IsNullOrEmpty(zone))
				return true;
			else if (AvailableZones != null)
				foreach (AvailableZoneAttribute a in AvailableZones)
					if (a.ZoneName == zone)
						return true;
			return false;
		}

		/// <summary>Used to determine wether a child definition is allowed in a zone.</summary>
		public bool IsAllowedInZone(string zone, ICollection<string> allowed)
		{
			if (string.IsNullOrEmpty(zone))
			{
				if (allowed == null)
					return true;
				else if (allowed.Count == 0)
					return true;
				else if (allowed.Contains(string.Empty))
					return true;
			}
			foreach (AvailableZoneAttribute available in AvailableZones)
				if (allowed == null || allowed.Contains(available.ZoneName) && available.ZoneName == zone)
					return true;
			return false;
		}

		public bool IsAuthorized(IPrincipal user)
		{
			if (user == null || authorizedRoles == null)
				return true;
			foreach (string role in authorizedRoles)
				if (string.Equals(user.Identity.Name, role, StringComparison.OrdinalIgnoreCase) || user.IsInRole(role))
					return true;
			return false;
		}

		/// <summary>Find out if this item allows sub-items of a certain type.</summary>
		/// <param name="child">The item that should be checked whether it is allowed below this item.</param>
		/// <returns>True if the specified child item is allowed below this item.</returns>
		public bool IsChildAllowed(ItemDefinition child)
		{
			return AllowedChildren.Contains(child);
		}

		#region IComparable<ItemDefinition> Members

		/// <summary>Compares the sort order of this item definition to another.</summary>
		/// <param name="other">The other item definition to compare.</param>
		/// <returns>This items sort order compared to other definition's.</returns>
		public int CompareTo(ItemDefinition other)
		{
			return SortOrder - other.SortOrder;
		}

		#endregion

		#region Equals, GetHashCode & ToString Methods

		public override string ToString()
		{
			return Discriminator;
		}

		public override int GetHashCode()
		{
			return Discriminator.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is ItemDefinition)
				return (Discriminator.Equals((obj as ItemDefinition).Discriminator));
			else
				return false;
		}

		#endregion

		/// <summary>Adds an allowed child definition to the list of allowed definitions.</summary>
		/// <param name="definition">The allowed child definition to add.</param>
		public void AddAllowedChild(ItemDefinition definition)
		{
			if(!AllowedChildren.Contains(definition))
				AllowedChildren.Add(definition);
		}

		/// <summary>Removes an allowed child definition from the list of allowed definitions if not already removed.</summary>
		/// <param name="definition">The definition to remove.</param>
		public void RemoveAllowedChild(ItemDefinition definition)
		{
			if (AllowedChildren.Contains(definition))
				AllowedChildren.Remove(definition);
		}

		/// <summary>Adds an allowed zone to the definition's list of allwed zones.</summary>
		/// <param name="zone">The zone name to add.</param>
		public void AddAllowedZone(string zone)
		{
			if (!AllowedZoneNames.Contains(zone))
				AllowedZoneNames.Add(zone);
		}

		/// <summary>Adds an containable editor or container to existing editors and to a container.</summary>
		/// <param name="containable">The editable to add.</param>
		public void Add(IContainable containable)
		{
			if(string.IsNullOrEmpty(containable.ContainerName))
			{
				RootContainer.AddContained(containable);
				AddToCollection(containable);
			}
			else
			{
				foreach(IEditableContainer container in Containers)
				{
					if(container.Name == containable.ContainerName)
					{
						container.AddContained(containable);
						AddToCollection(containable);
						return;
					}
				}
				throw new N2Exception(
					"The editor '{0}' references a container '{1}' which doesn't seem to be defined on '{2}'. Either add a container with this name or remove the reference to that container.",
					containable.Name, containable.ContainerName, ItemType);
			}
		}

		private void AddToCollection(IContainable containable)
		{
			if (containable is IEditable)
				Editables.Add(containable as IEditable);
			else if(containable is IEditableContainer)
				Containers.Add(containable as IEditableContainer);
		}
	}
}
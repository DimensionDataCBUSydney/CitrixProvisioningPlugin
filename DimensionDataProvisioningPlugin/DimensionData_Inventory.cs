﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Citrix.MachineCreationAPI;
using Citrix.ManagedMachineAPI;
using DD.CBU.Compute.Api.Client;
using DD.CBU.Compute.Api.Contracts.Image;
using DD.CBU.Compute.Api.Contracts.Network20;

namespace DimensionDataProvisioningPlugin
{
    /// <summary>
    /// A very minimal demo implementation of an inventory tree.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The first thing to understand about this demo code is that it just returns hard-wired "dummy" data, without
    /// querying the back-end hosting infrastructure at all. A real plugin will always need to replace this code in order
    /// to make calls to the infrastructure/cloud to actually gather the objects that need to be modelled in a tree.
    /// </para>
    /// <para>
    /// The inventory model is a tree of objects, similar to a filesystem. The tree needn't have a complex structure.
    /// The structure of the tree will often be suggested by the natural structure of zones, pools, clusters and other
    /// resources/objects on the infrastructure.
    /// </para>
    /// <para>
    /// This demo inventory tree assumes a completely flat structure: all objects are direct children of the root.
    /// And we only have three types of object: networks, storage pools and templates (images used as a basis for
    /// provisioning). This is enough to allow the plugin to support the creation of a hosting connection.
    /// </para>
    /// </remarks>
    public partial class DimensionDataService : IInventoryProvider
    {
        private const string RootFolderPath = "/";

        private readonly Dictionary<string, string> storagePools = new Dictionary<string, string>();
 
        /// <summary>
        /// Dummy extension data dictionary attached to all inventory items. Has no entries.
        /// </summary>
        /// <remarks>
        /// In the Provisioning SDK, all inventory items can be annotated with "extension data", in the form
        /// of arbitrary string key/value pairs. This allows all inventory items to carry a payload of additional,
        /// vendor-specific properties throughout the rest of the system. They can even be presented in the
        /// UI.
        /// </remarks>
        private readonly Dictionary<string, string> nullExtensionData = new Dictionary<string, string>();
 
        /// <summary>
        /// Gets all direct children of a given path.
        /// </summary>
        /// <param name="connectionSettings">The settings needed to contact and authenticate with the hosting
        /// infrastructure. This demo code doesn't consult the connection settings, because the inventory being
        /// returned is just hard-coded dummy data. A "real" implementation would use these settings to make the
        /// API calls needed to discover the inventory.</param>
        /// <param name="path">The path whose direct children are required.</param>
        /// <returns>
        /// The return value is a flat list of items, whose tree structure is implied by the path of each item. The
        /// plugin can choose to return all recursive children here, or just the direct children of the given path.
        /// This dummy implementation assumes a simple, flat tree, where all networks/storage/templates are direct
        /// children of the root. Therefore, it only returns any data when "/" is passed in as the query path.
        /// If you use Powershell to do a "dir" within the XDHyp:\Connections\ tree, you'll be able to see
        /// these items.
        /// </returns>
        public IList<IInventoryItem> GetContents(ConnectionSettings connectionSettings, string path)
        {
            List<IInventoryItem> inventoryItems = new List<IInventoryItem>();
            if (path == RootFolderPath)
            {
                ComputeApiClient client = connectionSettings.GetComputeClient();
                IEnumerable<NetworkDomainType> networkDomains = client.Networking.NetworkDomain.GetNetworkDomains().Result;
                AddNetworks(path, inventoryItems, networkDomains);
                //AddStoragePools(path, inventoryItems, storage);
                var images = client.ServerManagementLegacy.ServerImage.GetImages(null, null, null, null, null).Result;
                AddTemplates(path, inventoryItems, images);
            }
            return inventoryItems;
        }

        public IInventoryItem GetItemFromPath(ConnectionSettings connectionSettings, string path)
        {
            logger.TraceMsg("GetItemFromPath({0})", path);

            if (path == RootFolderPath)
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                dict.Add("IsSymLink", bool.TrueString);

                logger.TraceMsg("Returning degenerate root item.");

                // The root path "/" does not refer to a concrete item, but it is a valid path, so don't fail if a query is made.
                // In practice, this query is only ever made to check that the root path is valid, rather than to care about the
                // item returned.
                return new InventoryItem(
                    "root",
                    "root",
                    "root",
                    path,
                    true,
                    dict);
            }

            // Brute-force demo implementation. Just list the entire inventory, and return the item that has the path we're
            // looking for.
            return GetContents(connectionSettings, RootFolderPath).FirstOrDefault(item => item.FullPath == path);
        }

        public IInventoryItem GetItemFromId(ConnectionSettings connectionSettings, string id, string type)
        {
            // Brute-force demo implementation. Just list the entire inventory, and return the item that has the ID we're
            // looking for.
            return GetContents(connectionSettings, RootFolderPath).FirstOrDefault(item => item.Id == id);
        }

        internal void GatherInventory()
        {
            // Nothing to see here.
        }

        private void AddNetworks(string containingPath, List<IInventoryItem> items, IEnumerable<NetworkDomainType> networkDomains)
        {
            items.AddRange(networkDomains.Select(network => new InventoryItem(network.name, InventoryItemTypes.Network, network.id, containingPath, false, nullExtensionData)).Cast<IInventoryItem>());
        }

        private void AddStoragePools(string containingPath, List<IInventoryItem> items)
        {
            foreach (KeyValuePair<string, string> storagePool in storagePools)
            {
                var item = new InventoryItem(
                    storagePool.Value,
                    InventoryItemTypes.Storage,
                    storagePool.Key,
                    containingPath,
                    false,
                    nullExtensionData);

                items.Add(item);
            }
        }

        private void AddTemplates(string containingPath, List<IInventoryItem> items, IReadOnlyList<ImagesWithDiskSpeedImage> images)
        {
            items.AddRange(images.Select(image => new InventoryItem(image.name, InventoryItemTypes.Template, image.id, containingPath, false, nullExtensionData)).Cast<IInventoryItem>());
        }
    }
}

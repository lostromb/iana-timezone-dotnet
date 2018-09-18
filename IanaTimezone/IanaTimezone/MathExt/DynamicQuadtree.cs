using System;
using System.Collections.Generic;
using System.Linq;

namespace Iana.Timezone.MathExt
{
    /// <summary>
    /// A quadtree (2D vector containment structure) that resizes itself dynamically to whatever boundaries it needs
    /// for the objects that it contains.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class DynamicQuadtree<T>
    {

        private const int COMPRESSION_INTERVAL = 100;

        private QuadtreeNode<T> _root;

        private IDictionary<T, Vector2f> _itemLocations;

        // Used to lazily resize the tree based on its data
        private int _compressionCounter = 0;

        /// <summary>
        /// Creates a new dynamic quad tree. The tree will span any arbitrary space,
        /// adjusting its size automatically based on its input data
        /// </summary>
        public DynamicQuadtree() : this(new Rect2f(0, 0, 1, 1))
        {
        }

        /// <summary>
        /// Creates a new dynamic quad tree, with a hint to indicate its probable
        /// maximum bounds.
        /// </summary>
        /// <param name="initialBounds"></param>
        public DynamicQuadtree(Rect2f initialBounds)
        {
            _root = new QuadtreeNode<T>(this, null, -1);
            _itemLocations = new Dictionary<T, Vector2f>();
            _root.SetBounds(initialBounds);
        }

        /// <summary>
        /// Returns the node that is at the specified point, at the lowest level on the tree.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private QuadtreeNode<T> GetContainingNode(Vector2f point)
        {
            if (!_root.IsSubdivided)
                return _root;
            else if (!_root.Contains(point))
                return _root;
            else
                return _root.GetContainingNode(point); //Start the recursion
        }

        /// <summary>
        /// Finds and returns the smallest node that contains the entire specified cube.
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        private QuadtreeNode<T> GetContainingNode(Rect2f rect)
        {
            if (!_root.IsSubdivided) //Case: Root is the only node in the map
                return _root;
            else if (!_root.Contains(rect))
            {
                return _root; //Case: No quad is large enough to contain the cube - return the root
            }
            else
            {
                // Cycle through and call the recursive function on the root's children
                for (int child = 0; child < 4; child++)
                {
                    if (_root.GetChild(child).Contains(rect))
                    {
                        return _root.GetChild(child).GetContainingNode(rect);
                    }
                }

                return _root; // Case: Root quad contains the rectangle but none of its children do - return root
            }
        }

        /// <summary>
        /// Called privately by the child nodes when an item has been removed. Used to synchronize 
        /// the overall tree's item records with the fact that the item is now gone.
        /// </summary>
        /// <param name="toRemove"></param>
        protected void RemoveItemRecord(T toRemove)
        {
            _itemLocations.Remove(toRemove);
        }

        /// <summary>
        /// Increases the size of this tree recursively until it contains the specified point.
        /// </summary>
        /// <param name="target"></param>
        private void ExpandOutTo(Vector2f target)
        {
            while (!_root.Contains(target))
            {
                QuadtreeNode<T> newRoot = new QuadtreeNode<T>(this, null, -1);
                newRoot.Weight = _root.Weight;

                bool right = target.X > _root.GetBounds().MaxX;
                bool bottom = target.Y > _root.GetBounds().MaxY;
                
                if (!right && !bottom) // Up and left
                {
                    _root.Parent = newRoot;
                    _root.ParentId = 3;
                    newRoot.SetBounds(new Rect2f(
                            _root.X - _root.Width,
                            _root.Y - _root.Height,
                            _root.Width * 2f,
                            _root.Height * 2f));
                    newRoot.Subdivide();
                    newRoot.SetChild(3, _root);
                    _root = newRoot;
                }
                else if (!right && bottom) // Down and left
                {
                    _root.Parent = newRoot;
                    _root.ParentId = 1;
                    newRoot.SetBounds(new Rect2f(
                            _root.X - _root.Width,
                            _root.Y,
                            _root.Width * 2f,
                            _root.Height * 2f));
                    newRoot.Subdivide();
                    newRoot.SetChild(1, _root);
                    _root = newRoot;
                }
                else if (right && !bottom) //Up and right
                {
                    _root.Parent = newRoot;
                    _root.ParentId = 2;
                    newRoot.SetBounds(new Rect2f(
                            _root.X,
                            _root.Y - _root.Height,
                            _root.Width * 2f,
                            _root.Height * 2f));
                    newRoot.Subdivide();
                    newRoot.SetChild(2, _root);
                    _root = newRoot;
                }
                else if (right && bottom) //Down and right
                {
                    _root.Parent = newRoot;
                    _root.ParentId = 0;
                    newRoot.SetBounds(new Rect2f(
                            _root.X,
                            _root.Y,
                            _root.Width * 2f,
                            _root.Height * 2f));
                    newRoot.Subdivide();
                    newRoot.SetChild(0, _root);
                    _root = newRoot;
                }
            }
        }

        /// <summary>
        /// Checks to see if the tree is much larger than it needs to be, and if so, reduces its size
        /// </summary>
        private void CompressTree()
        {
            if (_root.IsSubdivided && _compressionCounter++ >= COMPRESSION_INTERVAL)
            {
                _compressionCounter = 0;

                // Does one of the root's children hold all of the mass?
                for (int child = 0; child < 4; child++)
                {
                    if (_root.GetChild(child).Weight == _root.Weight)
                    {
                        // Make that the new root
                        _root = _root.GetChild(child);
                        _root.ParentId = -1;
                        _root.Parent = null;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Adds the specified item to the tree at the specified point.
        /// </summary>
        /// <param name="toAdd"></param>
        /// <param name="location"></param>
        public void AddItem(T toAdd, Vector2f location)
        {
            if (_itemLocations.ContainsKey(toAdd)) // Don't double-add
            {
                //Console.Error.WriteLine("Can't double-add an item to the tree!");
                return;
            }
            _itemLocations[toAdd] = location;
            //Expand the tree to accommodate the new item, if necessary
            if (!_root.Contains(location))
            {
                ExpandOutTo(location);
            }
            _root.AddItem(new Tuple<T, Vector2f>(toAdd, location));
        }

        /// <summary>
        /// Removes the specified item from the tree.
        /// </summary>
        /// <param name="toRemove"></param>
        public void RemoveItem(T toRemove)
        {
            if (_itemLocations.ContainsKey(toRemove))
            {
                // Hone in on the item
                QuadtreeNode<T> node = GetContainingNode(_itemLocations[toRemove]);

                // Do the removal
                node.RemoveItem(toRemove);

                // Compress the tree if we can
                CompressTree();
            }
        }

        /// <summary>
        /// Returns the Point associated with the specified item in the tree, or null 
        /// if the item is not in the tree.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Vector2f GetLocationOf(T item)
        {
            return _itemLocations[item];
        }

        /// <summary>
        /// Returns the collection of all items in this tree, and their respective locations.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Tuple<T, Vector2f>> GetAllItems()
        {
            return _root.GetItems();
        }

        /// <summary>
        /// Returns the total number of items in the tree.
        /// </summary>
        /// <returns></returns>
        public int Count
        {
            get
            {
                return _root.Weight;
            }
        }

        /// <summary>
        /// Returns the list of all items in the tree that fall within the specified boundaries, 
        /// paired with their tree locations.
        /// </summary>
        /// <param name="boundaries"></param>
        /// <returns></returns>
        public List<Tuple<T, Vector2f>> GetItemsInRect(Rect2f boundaries)
        {
            // Find the node that contains the entire specified rectangle
            QuadtreeNode<T> headNode = GetContainingNode(boundaries);

            // Get its items
            IEnumerable<Tuple<T, Vector2f>> items = headNode.GetItems();

            // And filter out the ones that are outside the rectangle.
            List<Tuple<T, Vector2f>> returnVal = new List<Tuple<T, Vector2f>>();
            foreach (Tuple<T, Vector2f> item in items)
            {
                if (boundaries.Contains(item.Item2))
                    returnVal.Add(item);
            }

            return returnVal;
        }

        private Vector2f PutVectorInBounds(Vector2f point)
        {
            if (!_root.Contains(point))
            {
                if (point.X < _root.GetBounds().X)
                    point.SetLocation(_root.GetBounds().X, point.Y);
                else if (point.X >= _root.GetBounds().MaxX)
                    point.SetLocation(_root.GetBounds().X + _root.GetBounds().Width * 0.99f, point.Y);

                if (point.Y < _root.GetBounds().Y)
                    point.SetLocation(point.X, _root.GetBounds().Y);
                else if (point.Y >= _root.GetBounds().MaxY)
                    point.SetLocation(point.X, _root.GetBounds().Y + _root.GetBounds().Height * 0.99f);
            }

            return point;
        }

        /// <summary>
        /// Returns a handful of items that are near the specified point in the tree.
        /// The actual nearest item is NOT GUARANTEED to be in the returned set
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public IList<Tuple<T, Vector2f>> GetItemsNearPointOld(Vector2f point)
        {
            // If the test point it outside of the tree, move it to the inside
            point = PutVectorInBounds(point);
            QuadtreeNode<T> bottomNode = GetContainingNode(point);
            List<Tuple<T, Vector2f>> returnVal = new List<Tuple<T, Vector2f>>();
            returnVal.AddRange(GetItemsInLowestBin(point));

            // Union the result with bins that are adjacent to the current one, in case the test point
            // is on the edge of the bin and there could be closer points on the other side
            returnVal.AddRange(GetItemsInLowestBin(new Vector2f(point.X + bottomNode.Width, point.Y)));
            returnVal.AddRange(GetItemsInLowestBin(new Vector2f(point.X - bottomNode.Width, point.Y)));
            returnVal.AddRange(GetItemsInLowestBin(new Vector2f(point.X, point.Y + bottomNode.Height)));
            returnVal.AddRange(GetItemsInLowestBin(new Vector2f(point.X, point.Y - bottomNode.Height)));

            return returnVal;
        }

        public IList<Tuple<T, Vector2f>> GetItemsNearPoint(Vector2f point)
        {
            if (_root.Weight == 0)
            {
                return new List<Tuple<T, Vector2f>>();
            }

            // If the test point it outside of the tree, move it to the inside
            Vector2f inBoundsPoint = PutVectorInBounds(point);
            QuadtreeNode<T> bottomNode = GetContainingNode(point);
            float xDimension = bottomNode.Width;// + Math.Abs(inBoundsPoint.X - point.X);
            float yDimension = bottomNode.Height;// + Math.Abs(inBoundsPoint.Y - point.Y);
            IList<Tuple<T, Vector2f>> returnVal = null;
            while (returnVal == null || returnVal.Count == 0)
            {
                returnVal = GetItemsInRect(new Rect2f(
                    inBoundsPoint.X - xDimension,
                    inBoundsPoint.Y - yDimension,
                    xDimension * 2,
                    yDimension * 2));

                // If no items are found, expand the search broader and broader. We know the tree _does_ contain points so this must turn up something eventually
                xDimension *= 1.1f;
                yDimension *= 1.1f;
            }

            return returnVal;
        }

        private IEnumerable<Tuple<T, Vector2f>> GetItemsInLowestBin(Vector2f point)
        {
            QuadtreeNode<T> bottomNode = GetContainingNode(point);
            IEnumerable<Tuple<T, Vector2f>> returnVal = bottomNode.GetItems();
            while (returnVal.Count() == 0 && bottomNode.Parent != null)
            {
                bottomNode = bottomNode.Parent;
                returnVal = bottomNode.GetItems();
            }

            return returnVal;
        }

        /// <summary>
        /// Returns a handful of items that are near the specified point in the tree.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public List<Tuple<float, T>> GetItemsNearPointSorted(Vector2f point)
        {
            point = PutVectorInBounds(point);
            QuadtreeNode<T> bottomNode = GetContainingNode(point);
            List<Tuple<float, T>> returnVal = bottomNode.GetItemsSorted(point);
            while (returnVal.Count == 0 && bottomNode.Parent != null)
            {
                bottomNode = bottomNode.Parent;
                returnVal = bottomNode.GetItemsSorted(point);
            }

            returnVal.Sort((a, b) => a.Item1.CompareTo(b.Item1));

            return returnVal;
        }

        /// <summary>
        /// Returns a handful of items that are near the specified point in the tree, within a certain distance
        /// </summary>
        /// <returns></returns>
        public IList<Tuple<T, Vector2f>> GetItemsNearPoint(Vector2f point, float maxDistance)
        {
            point = PutVectorInBounds(point);
            QuadtreeNode<T> bottomNode = GetContainingNode(point);
            IList<Tuple<T, Vector2f>> returnVal = bottomNode.GetItemsNear(point, maxDistance);
            while (returnVal.Count == 0 && bottomNode.Parent != null)
            {
                bottomNode = bottomNode.Parent;
                returnVal = bottomNode.GetItemsNear(point, maxDistance);
            }
            return returnVal;
        }

        /// <summary>
        /// Represents a single subdivided square within a tree.
        /// </summary>
        /// <typeparam name="E"></typeparam>
        private class QuadtreeNode<E>
        {
            private const int INITIAL_SUBDIVIDE_THRESHOLD = 100;
            private const int SUBDIVIDE_MULTIPLY_FACTOR = 2;

            private int _subdivisionThreshold;
            private DynamicQuadtree<E> _containingTree;
            private QuadtreeNode<E>[] _children;
            private Rect2f _bounds;
            private bool _isSubdivided;
            private Dictionary<E, Tuple<E, Vector2f>> _data;
            private int _weight;

            // Cached variables for speed
            private float _centerX;
            private float _centerY;
            
            public QuadtreeNode(DynamicQuadtree<E> tree, QuadtreeNode<E> par, int ID)
            {
                // Everything but the quad size is initialized here in the constructor.
                _containingTree = tree;
                Parent = par;
                ParentId = ID;
                _isSubdivided = false;
                _children = new QuadtreeNode<E>[4];
                _weight = 0;
                _data = new Dictionary<E, Tuple<E, Vector2f>>();
                _subdivisionThreshold = (par == null ? INITIAL_SUBDIVIDE_THRESHOLD : par.SubdivisionThreshold);
            }

            public void SetBounds(Rect2f newBounds)
            {
                _bounds = newBounds;
                _centerX = _bounds.CenterX;
                _centerY = _bounds.CenterY;
            }

            /// <summary>
            /// Returns true if this node is divided into 4 child nodes.
            /// </summary>
            public bool IsSubdivided
            {
                get
                {
                    return _isSubdivided;
                }
            }

            public QuadtreeNode<E> GetChild(int i)
            {
                return _children[i];
            }

            public void SetChild(int i, QuadtreeNode<E> newChild)
            {
                _children[i] = newChild;
            }

            /// <summary>
            /// The "parentID" is the index of this child within its parent quad. For example,
            /// if a node were child 0 of its parent, then the child's parentID would be 0.
            /// In other words, parent.getChild[child.parentID] = child
            /// </summary>
            public int ParentId
            {
                get;
                set;
            }

            /// <summary>
            /// Returns the quadmapnode that contains this one, or null if this is root.
            /// </summary>
            public QuadtreeNode<E> Parent
            {
                get;
                set;
            }

            /// <summary>
            /// The width of this node's bounds
            /// </summary>
            public float Width
            {
                get
                {
                    return _bounds.Width;
                }
            }

            /// <summary>
            ///  The height of this node's bounds
            /// </summary>
            /// <returns></returns>
            public float Height
            {
                get
                {
                    return _bounds.Height;
                }
            }

            /// <summary>
            /// Returns the weight of this node (which is the count of all objects 
            /// that are within its boundaries)
            /// </summary>
            public int Weight
            {
                get
                {
                    return _weight;
                }
                set
                {
                    _weight = value;
                }
            }

            /// <summary>
            /// Returns the subdivision threshold, which is the number of items that 
            /// must be within this node before it will trigger subdivision.
            /// </summary>
            private int SubdivisionThreshold
            {
                get
                {
                    return _subdivisionThreshold;
                }
            }

            /// <summary>
            /// Returns the coordinate of the x origin of this quad.
            /// </summary>
            public float X
            {
                get
                {
                    return _bounds.X;
                }
            }

            /// <summary>
            /// Returns the coordinate of the x origin of this quad.
            /// </summary>
            public float Y
            {
                get
                {
                    return _bounds.Y;
                }
            }

            /// <summary>
            /// Removes a specific item from this node. If the item is not directly
            /// contained in this node (i.e. this is not the lowest-level node)
            /// then this method will do nothing.
            /// </summary>
            /// <param name="toRemove"></param>
            public void RemoveItem(E toRemove)
            {
                if (_data != null && _data.ContainsKey(toRemove))
                {
                    _data.Remove(toRemove);
                    _containingTree.RemoveItemRecord(toRemove);

                    // Propagate the weight difference up to the root
                    PropagateWeightChangeUp();
                }
            }

            protected void PropagateWeightChangeUp()
            {
                _weight--;
                // Coalesce on the way up
                if (_weight < _subdivisionThreshold)
                    Coalesce();
                if (Parent != null)
                    Parent.PropagateWeightChangeUp();
            }

            /**
             * Adds the specified item to this node. This method carries the important
             * precondition that this node already contains() the specified point.
             * If necessary, this node will be subdivided to compensate.
             * @param newItem The item to be added, paired with a location.
             */
            public void AddItem(Tuple<E, Vector2f> newItem)
            {
                _weight += 1;
                if (!_isSubdivided)
                {
                    _data[newItem.Item1] = newItem;
                    if (_weight > _subdivisionThreshold)
                        Subdivide();
                }
                else
                {
                    if (newItem.Item2.X < _centerX)
                    {
                        if (newItem.Item2.Y < _centerY)
                            _children[0].AddItem(newItem);
                        else
                            _children[2].AddItem(newItem);
                    }
                    else
                    {
                        if (newItem.Item2.Y < _centerY)
                            _children[1].AddItem(newItem);
                        else
                            _children[3].AddItem(newItem);
                    }
                }
            }

            /**
             * Divides this node into 4 child nodes. All objects in this node will be
             * offloaded onto the children
             */
            public void Subdivide()
            {
                if (!IsSubdivided)
                {
                    // Detect if this node is involved in infinite recursion
                    if (Parent != null && _weight == Parent.Weight)
                        _subdivisionThreshold = Parent.SubdivisionThreshold * SUBDIVIDE_MULTIPLY_FACTOR;

                    _isSubdivided = true;
                    for (int c = 0; c < 4; c++)
                        _children[c] = new QuadtreeNode<E>(_containingTree, this, c);

                    float halfWidth = Width / 2f;
                    float halfHeight = Height / 2f;
                    _children[0].SetBounds(new Rect2f(X, Y, halfWidth, halfHeight));
                    _children[1].SetBounds(new Rect2f(X + halfWidth, Y, halfWidth, halfHeight));
                    _children[2].SetBounds(new Rect2f(X, Y + halfHeight, halfWidth, halfHeight));
                    _children[3].SetBounds(new Rect2f(X + halfWidth, Y + halfHeight, halfWidth, halfHeight));

                    // Add this node's items to the children
                    foreach (Tuple<E, Vector2f> item in _data.Values)
                    {
                        if (item.Item2.X < _centerX)
                        {
                            if (item.Item2.Y < _centerY)
                                _children[0].AddItem(item);
                            else
                                _children[2].AddItem(item);
                        }
                        else
                        {
                            if (item.Item2.Y < _centerY)
                                _children[1].AddItem(item);
                            else
                                _children[3].AddItem(item);
                        }
                    }

                    // Delete the local storage of this node.
                    _data.Clear();
                    _data = null;
                }
                else
                    throw new InvalidOperationException("Cannot subdivide a divided quad!");
            }

            /**
             * If this node has children, recursively delete them all.
             * Opposite of "subdivide".
             */
            private void Coalesce()
            {
                if (_isSubdivided)
                {
                    _isSubdivided = false;
                    for (int c = 0; c < 4; c++)
                        _children[c].Coalesce();

                    // Pull the objects owned by the children into this node
                    _data = new Dictionary<E, Tuple<E, Vector2f>>();
                    for (int c = 0; c < 4; c++)
                    {
                        _children[c].AddItemsTo(_data);
                        _children[c] = null;
                    }
                }
            }

            /**
             * Returns true if any part of this quad intersects the specified rectangle.
             */
            public bool Intersects(Rect2f rectangle)
            {
                return _bounds.Intersects(rectangle);
            }

            /**
             * Returns true iff this quad contains the entire rectangle.
             * Containment is inclusive on the top and left edges, and exclusive
             * on the right and bottom edges.
             */
            public bool Contains(Rect2f rect)
            {
                return _bounds.Contains(rect);
            }

            /**
             * Returns true iff this quad contains the specified point.
             * Containment is inclusive on the top and left edges, and exclusive
             * on the right and bottom edges.
             */
            public bool Contains(Vector2f point)
            {
                return _bounds.Contains(point);
            }

            /**
             * Returns the set of all items that are contained within this node and all
             * of its children
             * @return 
             */
            public IEnumerable<Tuple<E, Vector2f>> GetItems()
            {
                if (!_isSubdivided)
                {
                    return _data.Values;
                }
                else
                {
                    HashSet<Tuple<E, Vector2f>> returnVal = new HashSet<Tuple<E, Vector2f>>();
                    for (int c = 0; c < 4; c++)
                        _children[c].AddItemsTo(returnVal);
                    return returnVal;
                }
            }

            /*
             * ?The last hour is on us both?mr.s?tuck this little kitty into the impenetrable
             * brainpan?
             * pr?Contents under pressure?Do not expose to excessive heat, vacuum, blunt trauma,
             * immersion in liquids, disintegration, reintegration, hypersleep, humiliation, sorrow
             * or harsh language?
             * pr?When the time comes, whose life will flash before yours?
             */

            public IList<Tuple<E, Vector2f>> GetItemsNear(Vector2f point, float maxDist)
            {
                if (!_isSubdivided)
                {
                    IList<Tuple<E, Vector2f>> returnVal = new List<Tuple<E, Vector2f>>();
                    foreach (var item in _data)
                    {
                        float dist = point.Distance(item.Value.Item2);
                        if (dist < maxDist)
                            returnVal.Add(item.Value);
                    }
                    return returnVal;
                }
                else
                {
                    IList<Tuple<E, Vector2f>> returnVal = new List<Tuple<E, Vector2f>>();
                    for (int c = 0; c < 4; c++)
                        _children[c].AddItemsTo(returnVal, point, maxDist);
                    return returnVal;
                }
            }

            /**
             * Returns the set of all items that are contained within this node and all
             * of its children, sorted by their distance from the given vector
             * @return 
             */
            public List<Tuple<float, E>> GetItemsSorted(Vector2f testPoint)
            {
                List<Tuple<float, E>> returnVal = new List<Tuple<float, E>>();
                AddItemsTo(returnVal, testPoint);
                return returnVal;
            }

            /**
             * Adds all of the items in this node and all child nodes to the specified
             * set.
             * @param targetSet 
             */
            private void AddItemsTo(HashSet<Tuple<E, Vector2f>> targetSet)
            {
                if (!_isSubdivided)
                {
                    foreach (var val in _data.Values)
                    {
                        targetSet.Add(val);
                    }
                }
                else
                {
                    for (int c = 0; c < 4; c++)
                        _children[c].AddItemsTo(targetSet);
                }
            }

            /**
             * Adds all of the items in this node and all child nodes to the specified
             * map.
             * @param targetSet 
             */
            private void AddItemsTo(IDictionary<E, Tuple<E, Vector2f>> targetMap)
            {
                if (!_isSubdivided)
                {
                    foreach (Tuple<E, Vector2f> item in _data.Values)
                        targetMap[item.Item1] = item;
                }
                else
                {
                    for (int c = 0; c < 4; c++)
                        _children[c].AddItemsTo(targetMap);
                }
            }

            /**
             * Adds all of the items in this node and all child nodes to the specified
             * sorted list
             * @param targetSet 
             */
            private void AddItemsTo(List<Tuple<float, E>> targetList, Vector2f testPoint)
            {
                if (!_isSubdivided)
                {
                    foreach (var val in _data.Values)
                    {
                        float dist = testPoint.Distance(val.Item2);
                         targetList.Add(new Tuple<float, E>(dist, val.Item1));
                    }
                }
                else
                {
                    for (int c = 0; c < 4; c++)
                        _children[c].AddItemsTo(targetList, testPoint);
                }
            }

            /**
             * Adds all of the items in this node and all child nodes to the specified
             * sorted list
             * @param targetSet 
             */
            private void AddItemsTo(IList<Tuple<E, Vector2f>> targetList, Vector2f testPoint, float maxDist)
            {
                if (!_isSubdivided)
                {
                    foreach (var val in _data.Values)
                    {
                        float dist = testPoint.Distance(val.Item2);
                        if (dist < maxDist)
                            targetList.Add(val);
                    }
                }
                else
                {
                    for (int c = 0; c < 4; c++)
                        _children[c].AddItemsTo(targetList, testPoint, maxDist);
                }
            }

            /// <summary>
            /// Recursive counterpart to QuadMap.getContainingNode().
            /// Finds the lowest node in the tree that contains the rectangle.
            /// </summary>
            /// <param name="rect"></param>
            /// <returns></returns>
            public QuadtreeNode<E> GetContainingNode(Rect2f rect)
            {
                if (!_isSubdivided)
                {
                    return this; //We hit the bottom of the tree - return this quad
                }
                else
                {
                    if (rect.X < _bounds.X ||
                            rect.Y < _bounds.Y ||
                            rect.MaxX >= _bounds.MaxX ||
                            rect.MaxY >= _bounds.MaxY)
                    {
                        return null; // Rectangle not in this quad; fail
                    }

                    if (rect.MaxX < _centerX)
                    {
                        // rectangle falls in the left side of the quad
                        if (rect.MaxY < _centerY)
                            return _children[0].GetContainingNode(rect);
                        else if (rect.Y >= _centerY)
                            return _children[2].GetContainingNode(rect);
                        else
                            return this;
                    }
                    else if (rect.X >= _centerX)
                    {
                        // rectangle falls in the right side of the quad
                        if (rect.MaxY < _centerY)
                            return _children[1].GetContainingNode(rect);
                        else if (rect.Y >= _centerY)
                            return _children[3].GetContainingNode(rect);
                        else
                            return this;
                    }
                    else
                    {
                        // rectangle straddles the vertical midpoint
                        return this;
                    }
                }
            }

            //Recursive counterpart to QuadMap.getContainingNode()
            public QuadtreeNode<E> GetContainingNode(Vector2f point)
            {
                if (!_isSubdivided)
                    return this; //Case: We hit the lowest tree node that contains the point - this is the one we want
                else
                {
                    if (point.X < _centerX)
                    {
                        if (point.Y < _centerY)
                            return _children[0].GetContainingNode(point);
                        else if (point.Y < _bounds.Y + _bounds.Height)
                            return _children[2].GetContainingNode(point);
                    }
                    else
                    {
                        if (point.Y < _centerY)
                            return _children[1].GetContainingNode(point);
                        else if (point.Y < _bounds.Y + _bounds.Height)
                            return _children[3].GetContainingNode(point);
                    }
                }
                return null; // Point not within this quad
            }

            /// <summary>
            /// Returns the boundary rectangle of this node.
            /// </summary>
            /// <returns></returns>
            public Rect2f GetBounds()
            {
                return _bounds;
            }
        }
    }
}

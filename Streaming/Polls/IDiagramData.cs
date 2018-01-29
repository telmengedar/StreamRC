using System.Collections.Generic;

namespace StreamRC.Streaming.Polls {

    /// <summary>
    /// interface for diagram data
    /// </summary>
    public interface IDiagramData {

        /// <summary>
        /// get items for diagram
        /// </summary>
        /// <param name="count">number of items to return at maximum</param>
        /// <returns>items to be displayed in diagram</returns>
        IEnumerable<DiagramItem> GetItems(int count = 5);
    }
}
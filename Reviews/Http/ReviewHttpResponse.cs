namespace StreamRC.Reviews.Http {
    public class ReviewHttpResponse {

        public HttpResultItem[] Items { get; set; }

        /// <summary>
        /// result value
        /// </summary>
        public int Result { get; set; }

        /// <summary>
        /// whether timeout is enabled
        /// </summary>
        public bool TimeoutEnabled { get; set; }
    }
}
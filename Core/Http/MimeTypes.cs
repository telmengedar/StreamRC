namespace StreamRC.Core.Http {

    /// <summary>
    /// operations for mime types
    /// </summary>
    public static class MimeTypes {

        /// <summary>
        /// get mime type for specified extension
        /// </summary>
        /// <remarks>
        /// only a small subset of filetypes is supported everything unknown is returned as application/octet-stream
        /// </remarks>
        /// <param name="extension">extension with leading dot for which to get mime type</param>
        /// <returns>mime type of extension</returns>
        public static string GetMimeType(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return "application/octet-stream";

            switch (extension.ToLower())
            {
                case ".asp":
                    return "text/asp";
                case ".avi":
                    return "video/avi";
                case ".c":
                case ".c++":
                case ".cpp":
                case ".cs":
                case ".h":
                case ".jav":
                case ".java":
                case ".log":
                case ".pl":
                case ".text":
                case ".txt":
                    return "text/plain";
                case ".mp3":
                    return "audio/mpeg3";
                case ".mp2":
                case ".mpg":
                case ".mpeg":
                    return "video/mpeg";
                case ".pdf":
                    return "application/pdf";
                case ".class":
                    return "application/java";
                case ".doc":
                case ".docx":
                case ".word":
                    return "application/msword";
                case ".xls":
                case ".xlsx":
                case ".xlt":
                    return "application/excel";
                case ".eps":
                case ".ps":
                    return "application/postscript";
                case ".gz":
                case ".gzip":
                    return "application/x-gzip";

                case ".htm":
                case ".html":
                case ".htmls":
                    return "text/html";
                case ".xml":
                    return "application/xml";
                case ".zip":
                    return "application/zip";
                case ".png":
                    return "image/png";
                case ".js":
                    return "text/javascript";
                case ".json":
                    return "application/json";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".gif":
                    return "image/gif";
                case ".bmp":
                    return "image/bmp";
                case ".css":
                    return "text/css";
                case ".ppt":
                case ".pps":
                    return "application/mspowerpoint";
                case ".rt":
                case ".rtf":
                    return "text/richtext";
                case ".wav":
                    return "audio/wav";
                default:
                    return "application/octet-stream";
            }
        }
    }
}
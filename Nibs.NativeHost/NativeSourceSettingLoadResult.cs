using System;

namespace Nibs.NativeHost {
	public class NativeSourceSettingLoadResult : INativeSourceSettingLoadResult {
		public NativeSourceSettingLoadResult(INativeSourceSettings settings, string? error = null, Exception? exception = null) {
			Settings = settings;
			Error = error;
			Exception = exception;
		}
		public bool HasError => Error != null || Exception != null;
		public string? Error { get; private set; }
		public Exception? Exception { get; private set; }
		public INativeSourceSettings Settings { get; private set; }
	}

}
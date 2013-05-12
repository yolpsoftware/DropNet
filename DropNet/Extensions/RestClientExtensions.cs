using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using DropNet.Exceptions;

namespace DropNet.Extensions
{
	public static class RestClientExtensions
	{
		public static Task<TResult> ExecuteTask<TResult>(
			this IRestClient client, IRestRequest request, CancellationToken token = default(CancellationToken)
			) where TResult : new()
		{
			var tcs = new TaskCompletionSource<TResult>();
			try {
				var async = client.ExecuteAsync<TResult>(request, (response, _) => {
					if (token.IsCancellationRequested || response == null)
						return;
					
					if (response.StatusCode != HttpStatusCode.OK) {
						tcs.TrySetException(new DropboxException(response));
					} else {
						tcs.TrySetResult(response.Data);
					}
				});
				
				token.Register(() => {
					// Crashes on the device: see https://bugzilla.xamarin.com/show_bug.cgi?id=8407
					// async.Abort();
					tcs.TrySetCanceled();
				});
			} catch (Exception ex) {
				tcs.TrySetException(ex);
			}
			
			return tcs.Task;
		}
		
		public static Task<IRestResponse> ExecuteTask(this IRestClient client, IRestRequest request, CancellationToken token = default(CancellationToken))
		{
			var tcs = new TaskCompletionSource<IRestResponse>();
			try {
				var async = client.ExecuteAsync(request, (response, _) => {
					if (token.IsCancellationRequested || response == null)
						return;
					
					if (response.StatusCode != HttpStatusCode.OK) {
						tcs.TrySetException(new DropboxException(response));
					} else {
						tcs.TrySetResult(response);
					}
				});
				
				token.Register(() => {
					// Crashes on the device: see https://bugzilla.xamarin.com/show_bug.cgi?id=8407
					// async.Abort();
					tcs.TrySetCanceled();
				});
			} catch (Exception ex) {
				tcs.TrySetException(ex);
			}
			
			return tcs.Task;
		}
	}
}
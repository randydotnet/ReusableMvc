﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using LazyMortal.Multipipeline;
using LazyMortal.Multipipeline.DecisionTree;
using LazyMortal.ReusableMvc.Options;
using LazyMortal.ReusableMvc.Pipelines;
using LazyMortal.ReusableMvc.Routes;
using LazyMortal.ReusableMvc.Views;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LazyMortal.ReusableMvc.Extensions
{
	public static class ReusableMvcApplicationBuilderExtensions
	{
		public static IApplicationBuilder UseReusableMvcWithDefaultRoute(this IApplicationBuilder app)
		{
			return app.UseReusableMvc(routes =>
			{
				routes.MapRoute(
					name: "reusable",
					template: "{area}/{controller=Home}/{action=Index}/{id?}");
				routes.MapRoute(
					name: "default",
					template: "{controller=Home}/{action=Index}/{id?}");
			});
		}

		public static IApplicationBuilder UseReusableMvc(this IApplicationBuilder app, Action<IRouteBuilder> configureRoutes)
		{
			app.ApplicationServices.GetRequiredService<IOptions<RazorViewEngineOptions>>()
				.Value.ViewLocationExpanders.Add(app.ApplicationServices.GetRequiredService<IReusableViewLocationExpander>());

			foreach (
				var reusablePipeline in
				app.ApplicationServices.GetRequiredService<PipelineCollectionAccessor>()
					.Pipelines.Cast<ReusablePipeline>()
					.Distinct())
			{
				reusablePipeline.ViewLocationTemplate =
					Regex.Replace(reusablePipeline.ViewLocationTemplate.Replace("{2}", reusablePipeline.Name), @"/{2,}", "/");
				reusablePipeline.SharedViewLocationTemplate =
					Regex.Replace(reusablePipeline.SharedViewLocationTemplate.Replace("{1}",
						reusablePipeline.Name), @"/{2,}", "/");
				reusablePipeline.StaticFilesLocationTemplate =
					Regex.Replace(reusablePipeline.StaticFilesLocationTemplate.Replace("{2}",
						reusablePipeline.Name.ToLower()), @"/{2,}", "/");
				reusablePipeline.ControllerFullNameTemplate =
					Regex.Replace(reusablePipeline.ControllerFullNameTemplate.Replace("{2}",
						reusablePipeline.Name), @"\.{2,}", ".");
			}

			app.UseMvc(routes =>
			{
				routes.DefaultHandler = routes.ServiceProvider.GetRequiredService<IReusableRouter>();
				configureRoutes(routes);
			});

			return app;
		}
	}
}
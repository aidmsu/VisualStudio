﻿using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GitHub.Extensions;
using GitHub.Models;
using GitHub.Primitives;
using GitHub.Services;
using GitHub.ViewModels.Dialog;
using NSubstitute;
using ReactiveUI;
using Xunit;

public class LoginCredentialsViewModelTests
{
    public class TheDoneSignal : TestBaseClass
    {
        [Fact]
        public async Task SucessfulGitHubLoginSignalsDone()
        {
            var connectionManager = Substitute.For<IConnectionManager>();
            var connection = Substitute.For<IConnection>();

            var gitHubLogin = Substitute.For<ILoginToGitHubViewModel>();
            var gitHubLoginCommand = ReactiveCommand.CreateAsyncObservable(_ =>
                Observable.Return(connection));
            gitHubLogin.Login.Returns(gitHubLoginCommand);
            var enterpriseLogin = Substitute.For<ILoginToGitHubForEnterpriseViewModel>();

            var loginViewModel = new LoginCredentialsViewModel(connectionManager, gitHubLogin, enterpriseLogin);
            var signalled = false;

            loginViewModel.Done.Subscribe(_ => signalled = true);
            await gitHubLoginCommand.ExecuteAsync();

            Assert.True(signalled);
        }

        [Fact]
        public async Task FailedGitHubLoginDoesNotSignalDone()
        {
            var connectionManager = Substitute.For<IConnectionManager>();

            var gitHubLogin = Substitute.For<ILoginToGitHubViewModel>();
            var gitHubLoginCommand = ReactiveCommand.CreateAsyncObservable(_ =>
                Observable.Return<IConnection>(null));
            gitHubLogin.Login.Returns(gitHubLoginCommand);
            var enterpriseLogin = Substitute.For<ILoginToGitHubForEnterpriseViewModel>();

            var loginViewModel = new LoginCredentialsViewModel(connectionManager, gitHubLogin, enterpriseLogin);
            var signalled = false;

            loginViewModel.Done.Subscribe(_ => signalled = true);
            await gitHubLoginCommand.ExecuteAsync();

            Assert.False(signalled);
        }

        [Fact]
        public async Task AllowsLoginFromEnterpriseAfterGitHubLoginHasFailed()
        {
            var connectionManager = Substitute.For<IConnectionManager>();
            var connection = Substitute.For<IConnection>();

            var gitHubLogin = Substitute.For<ILoginToGitHubViewModel>();
            var gitHubLoginCommand = ReactiveCommand.CreateAsyncObservable(_ =>
                Observable.Return<IConnection>(null));
            gitHubLogin.Login.Returns(gitHubLoginCommand);

            var enterpriseLogin = Substitute.For<ILoginToGitHubForEnterpriseViewModel>();
            var enterpriseLoginCommand = ReactiveCommand.CreateAsyncObservable(_ =>
                Observable.Return(connection));
            enterpriseLogin.Login.Returns(enterpriseLoginCommand);

            var loginViewModel = new LoginCredentialsViewModel(connectionManager, gitHubLogin, enterpriseLogin);
            var success = false;

            loginViewModel.Done
                .OfType<IConnection>()
                .Where(x => x != null)
                .Subscribe(_ => success = true);

            await gitHubLoginCommand.ExecuteAsync();
            await enterpriseLoginCommand.ExecuteAsync();

            Assert.True(success);
        }
    }

    public class TheLoginModeProperty : TestBaseClass
    {
        [Fact]
        public void LoginModeTracksAvailableConnections()
        {
            var connectionManager = Substitute.For<IConnectionManager>();
            var connections = new ObservableCollectionEx<IConnection>();
            var gitHubLogin = Substitute.For<ILoginToGitHubViewModel>();
            var enterpriseLogin = Substitute.For<ILoginToGitHubForEnterpriseViewModel>();
            var gitHubConnection = Substitute.For<IConnection>();
            var enterpriseConnection = Substitute.For<IConnection>();

            connectionManager.Connections.Returns(connections);
            gitHubConnection.HostAddress.Returns(HostAddress.GitHubDotComHostAddress);
            enterpriseConnection.HostAddress.Returns(HostAddress.Create("https://enterprise.url"));
            gitHubConnection.IsLoggedIn.Returns(true);
            enterpriseConnection.IsLoggedIn.Returns(true);

            var loginViewModel = new LoginCredentialsViewModel(connectionManager, gitHubLogin, enterpriseLogin);

            Assert.Equal(LoginMode.DotComOrEnterprise, loginViewModel.LoginMode);

            connections.Add(enterpriseConnection);
            Assert.Equal(LoginMode.DotComOnly, loginViewModel.LoginMode);

            connections.Add(gitHubConnection);
            Assert.Equal(LoginMode.None, loginViewModel.LoginMode);

            connections.RemoveAt(0);
            Assert.Equal(LoginMode.EnterpriseOnly, loginViewModel.LoginMode);
        }
    }
}

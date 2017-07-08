# How to setup local developement

Your local path to the Rimworld game & assemblies is probably not what anybody else is using.

To keep multiple developers from playing ping-pong with the project file(s) and the respective assembly HintPath entries that setting has been externalized.

After cloning the repository just edit `Source\RimworldInstall.props` (it's a plain XML file) and make sure the `RimworldManagedDir`entry resolves to the correct directory for _your_ local Rimworld assemblies. If you're developing on a running Rimworld install, then you can simply edit the `RimworldInstallDir` entry (other variables derive from there by default).
If you're keeping just a copy of the managed assemblies simply make sure the `RimworldManagedDir` resolves correctly to those assemblies. For relative paths starting with `$(SolutionDir)` is a good idea, as well as making sure you end in a `\`.

If you want to be a particularly conscientious developer, run

```bash
git update-index --skip-worktree Source\Rimworldinstall.props
```

after setting up your local version of the file. Then git will never upload your modified version.

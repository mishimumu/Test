
https://developer.android.com/studio/releases/gradle-plugin#updating-plugin
The Android Studio build system is based on Gradle, and the Android plugin for Gradle adds several features that are specific to building Android apps. Although the Android plugin is typically updated in lock-step with Android Studio, the plugin (and the rest of the Gradle system) can run independent of Android Studio and be updated separately.虽然Android插件通常与Android Studio锁定更新，但插件（以及Gradle系统的其他部分）可以独立于Android Studio运行，并可以单独更新

This page explains how to keep your Gradle tools up to date and what's in the recent updates.

For details about how to configure your Android builds with Gradle, see the following pages:

   Configure Your Build
   Android Plugin DSL Reference
   Gradle DSL Reference
For more information about the Gradle build system, see the Gradle user guide.

Update the Android Plugin for Gradle
When you update Android Studio, you may receive a prompt to automatically update the Android plugin for Gradle to the latest available version. You can choose to accept the update or manually specify a version based on your project's build requirements.

You can specify the Android plugin for Gradle version in either the File > Project Structure > Project menu in Android Studio, or the top-level build.gradle file. The plugin version applies to all modules built in that Android Studio project. The following example sets the Android plugin for Gradle to version 3.1.0 from the build.gradle file:

buildscript {
    repositories {
        // Gradle 4.1 and higher include support for Google's Maven repo using
        // the google() method. And you need to include this repo to download
        // Android plugin 3.0.0 or higher.
        google()
        ...
    }
    dependencies {
        classpath 'com.android.tools.build:gradle:3.1.0'
    }
}
Caution: You should not use dynamic dependencies in version numbers, such as 'com.android.tools.build:gradle:2.+'. Using this feature can cause unexpected version updates and difficulty resolving version differences.

If the specified plugin version has not been downloaded, Gradle downloads it the next time you build your project or click Tools > Android > Sync Project with Gradle Files from the Android Studio menu bar.

Update Gradle
When you update Android Studio, you may receive a prompt to also update Gradle to the latest available version. You can choose to accept the update or manually specify a version based on your project's build requirements.

The following table lists which version of Gradle is required for each version of the Android plugin for Gradle. For the best performance, you should use the latest possible version of both Gradle and the Android plugin.

Plugin version	Required Gradle version
1.0.0 - 1.1.3	2.2.1 - 2.3
1.2.0 - 1.3.1	2.2.1 - 2.9
1.5.0	2.2.1 - 2.13
2.0.0 - 2.1.2	2.10 - 2.13
2.1.3 - 2.2.3	2.14.1+
2.3.0+	3.3+
3.0.0+	4.1+
3.1.0+	4.4+
You can specify the Gradle version in either the File > Project Structure > Project menu in Android Studio, or by editing the Gradle distribution reference in the gradle/wrapper/gradle-wrapper.properties file. The following example sets the Gradle version to 4.4 in the gradle-wrapper.properties file.

...
distributionUrl = https\://services.gradle.org/distributions/gradle-4.4-all.zip
...
3.1.0 (March 2018)
This version of the Android plugin requires the following:

Gradle 4.4 or higher. To learn more, read the section about updating Gradle.
Build Tools 27.0.3 or higher. Keep in mind, you no longer need to specify a version for the build tools using the android.buildToolsVersion property—the plugin uses the minimum required version by default.
New DEX compiler, D8
By default, Android Studio now uses a new DEX compiler called D8. DEX compilation is the process of transforming .class bytecode into .dex bytecode for the Android Runtime (or Dalvik, for older versions of Android). Compared to the previous compiler, called DX, D8 compiles faster and outputs smaller DEX files, all while having the same or better app runtime performance.

D8 shouldn't change your day-to-day app development workflow. However, if you experience any issues related to the new compiler, please report a bug. You can temporarily disable D8 and use DX by including the following in your project's gradle.properties file:

android.enableD8=false
For projects that use Java 8 language features, incremental desugaring is enabled by default. You can disable it by specifying the following in your project's gradle.properties file:

android.enableIncrementalDesugaring=false.
Preview users: If you're already using a preview version of D8, note that it now compiles against libraries included in the SDK build tools—not the JDK. So, if you are accessing APIs that exist in the JDK but not in the SDK build tools libraries, you get a compile error.

Behavior changes
When building multiple APKs that each target a different ABI, the plugin no longer generates APKs for the following ABIs by default: mips, mips64, and armeabi.

If you want to build APKs that target these ABIs, you must use NDK r16b or lower and specify the ABIs in your build.gradle file, as shown below:

splits {
    abi {
        include 'armeabi', 'mips', 'mips64'
        ...
    }
}
When building configuration APKs for an Android Instant App, language configuration splits are now grouped by the root language by default. For example, if your app includes resources for zh-TW or zh-CN locales, Gradle will package those resources in a zh language configuration split. You can override this behavior by defining your own groups using the include property, as shown below:

splits {
    language {
        enable true
        // Each string defines a group of locales that
        // Gradle should package together.
        include "in,id",
                "iw,he",
                "fil,tl,tgl",
                "yue,zh,zh-TW,zh-CN"
    }
}
The Android plugin's build cache now evicts cache entries that are older than 30 days.

Passing "auto" to resConfig no longer automatically picks string resources to package into your APK. If you continue to use "auto", the plugin packages all string resources your app and its dependencies provide. So, you should instead specify each locale that you want the plugin to package into your APK.

Because local modules can't depend on your app's test APK, adding dependencies to your instrumented tests using the androidTestApi configuration, instead of androidTestImplementation, causes Gradle to issue the following warning:

  WARNING: Configuration 'androidTestApi' is obsolete
  and has been replaced with 'androidTestImplementation'
Fixes
Fixes an issue where Android Studio doesn't properly recognize dependencies in composite builds.
Fixes an issue where you get a project sync error when loading the Android plugin multiple times in a single build–for example, when multiple subprojects each include the Android plugin in their buildscript classpath.

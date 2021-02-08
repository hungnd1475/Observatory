/// <reference path='utils.ts'/>

namespace Platform {
    const Strings = Utils.Strings;
    const Arr = Utils.Arr;

    export interface PlatformInfo {
        name: string;
        versionRegexes: RegExp[];
        search: (uastring: string) => boolean;
    }

    export namespace PlatformInfo {
        const normalVersionRegex = /.*?version\/\ ?([0-9]+)\.([0-9]+).*/;

        function checkContains(target: string) {
            return (uastring: string) => {
                return Strings.contains(uastring, target);
            };
        };

        export const browsers: PlatformInfo[] = [
            {
                name: 'Edge',
                versionRegexes: [/.*?edge\/ ?([0-9]+)\.([0-9]+)$/],
                search: (uastring) => {
                    return Strings.contains(uastring, 'edge/') && Strings.contains(uastring, 'chrome') && Strings.contains(uastring, 'safari') && Strings.contains(uastring, 'applewebkit');
                }
            },
            {
                name: 'Chrome',
                versionRegexes: [/.*?chrome\/([0-9]+)\.([0-9]+).*/, normalVersionRegex],
                search: (uastring) => {
                    return Strings.contains(uastring, 'chrome') && !Strings.contains(uastring, 'chromeframe');
                }
            },
            {
                name: 'IE',
                versionRegexes: [/.*?msie\ ?([0-9]+)\.([0-9]+).*/, /.*?rv:([0-9]+)\.([0-9]+).*/],
                search: (uastring) => {
                    return Strings.contains(uastring, 'msie') || Strings.contains(uastring, 'trident');
                }
            },
            // INVESTIGATE: Is this still the Opera user agent?
            {
                name: 'Opera',
                versionRegexes: [normalVersionRegex, /.*?opera\/([0-9]+)\.([0-9]+).*/],
                search: checkContains('opera')
            },
            {
                name: 'Firefox',
                versionRegexes: [/.*?firefox\/\ ?([0-9]+)\.([0-9]+).*/],
                search: checkContains('firefox')
            },
            {
                name: 'Safari',
                versionRegexes: [normalVersionRegex, /.*?cpu os ([0-9]+)_([0-9]+).*/],
                search: (uastring) => {
                    return (Strings.contains(uastring, 'safari') || Strings.contains(uastring, 'mobile/')) && Strings.contains(uastring, 'applewebkit');
                }
            }
        ];

        export const oses: PlatformInfo[] = [
            {
                name: 'Windows',
                search: checkContains('win'),
                versionRegexes: [/.*?windows\ nt\ ?([0-9]+)\.([0-9]+).*/]
            },
            {
                name: 'iOS',
                search: (uastring) => {
                    return Strings.contains(uastring, 'iphone') || Strings.contains(uastring, 'ipad');
                },
                versionRegexes: [/.*?version\/\ ?([0-9]+)\.([0-9]+).*/, /.*cpu os ([0-9]+)_([0-9]+).*/, /.*cpu iphone os ([0-9]+)_([0-9]+).*/]
            },
            {
                name: 'Android',
                search: checkContains('android'),
                versionRegexes: [/.*?android\ ?([0-9]+)\.([0-9]+).*/]
            },
            {
                name: 'OSX',
                search: checkContains('mac os x'),
                versionRegexes: [/.*?mac\ os\ x\ ?([0-9]+)_([0-9]+).*/]
            },
            {
                name: 'Linux',
                search: checkContains('linux'),
                versionRegexes: []
            },
            {
                name: 'Solaris',
                search: checkContains('sunos'),
                versionRegexes: []
            },
            {
                name: 'FreeBSD',
                search: checkContains('freebsd'),
                versionRegexes: []
            },
            {
                name: 'ChromeOS',
                search: checkContains('cros'),
                versionRegexes: [/.*?chrome\/([0-9]+)\.([0-9]+).*/]
            }
        ];
    }

    export interface Version {
        major: number;
        minor: number;
    }

    export namespace Version {
        export function firstMatch(regexes: RegExp[], s: string): RegExp | undefined {
            for (let i = 0; i < regexes.length; i++) {
                const x = regexes[i];
                if (x.test(s)) {
                    return x;
                }
            }
            return undefined;
        };

        export function find(regexes: RegExp[], agent: string): Version {
            const r = firstMatch(regexes, agent);
            if (!r) {
                return { major: 0, minor: 0 };
            }
            const group = (i: number) => {
                return Number(agent.replace(r, '$' + i));
            };
            return nu(group(1), group(2));
        };

        export function detect(versionRegexes: RegExp[], agent: any): Version {
            const cleanedAgent = String(agent).toLowerCase();

            if (versionRegexes.length === 0) {
                return unknown();
            }
            return find(versionRegexes, cleanedAgent);
        };

        export function unknown(): Version {
            return nu(0, 0);
        };

        export function nu(major: number, minor: number): Version {
            return { major, minor };
        };
    }

    export interface UaString {
        current: string | undefined;
        version: Version;
    }

    export namespace UaString {
        export function detect(candidates: PlatformInfo[], userAgent: any): PlatformInfo {
            const agent = String(userAgent).toLowerCase();
            return Arr.find(candidates, candidate => candidate.search(agent));
        }

        export function detectBrowser(browsers: PlatformInfo[], userAgent: any): UaString {
            const browser = detect(browsers, userAgent);
            if (browser) {
                const version = Version.detect(browser.versionRegexes, userAgent);
                return {
                    current: browser.name,
                    version
                };
            }
            return null;
        };

        export function detectOs(oses: PlatformInfo[], userAgent: any): UaString {
            const os = detect(oses, userAgent);
            if (os) {
                const version = Version.detect(os.versionRegexes, userAgent);
                return {
                    current: os.name,
                    version
                };
            }
            return null;
        };
    }

    export interface Browser {
        readonly current: string | undefined;
        readonly version: Version;
        readonly isEdge: () => boolean;
        readonly isChrome: () => boolean;
        readonly isIE: () => boolean;
        readonly isOpera: () => boolean;
        readonly isFirefox: () => boolean;
        readonly isSafari: () => boolean;
    }

    export namespace Browser {
        const edge = 'Edge';
        const chrome = 'Chrome';
        const ie = 'IE';
        const opera = 'Opera';
        const firefox = 'Firefox';
        const safari = 'Safari';

        export function unknown(): Browser {
            return nu({
                current: undefined,
                version: Version.unknown()
            });
        };

        export function nu(info: UaString): Browser {
            const current = info.current;
            const version = info.version;

            const isBrowser = (name: string) => (): boolean => current === name;

            return {
                current,
                version,

                isEdge: isBrowser(edge),
                isChrome: isBrowser(chrome),
                isIE: isBrowser(ie),
                isOpera: isBrowser(opera),
                isFirefox: isBrowser(firefox),
                isSafari: isBrowser(safari)
            };
        };
    }

    export interface OperatingSystem {
        readonly current: string | undefined;
        readonly version: Version;
        readonly isWindows: () => boolean;
        readonly isiOS: () => boolean;
        readonly isAndroid: () => boolean;
        readonly isOSX: () => boolean;
        readonly isLinux: () => boolean;
        readonly isSolaris: () => boolean;
        readonly isFreeBSD: () => boolean;
        readonly isChromeOS: () => boolean;
    }

    export namespace OperatingSystem {
        const windows = 'Windows';
        const ios = 'iOS';
        const android = 'Android';
        const linux = 'Linux';
        const osx = 'OSX';
        const solaris = 'Solaris';
        const freebsd = 'FreeBSD';
        const chromeos = 'ChromeOS';

        export function unknown(): OperatingSystem {
            return nu({
                current: undefined,
                version: Version.unknown()
            });
        };

        export function nu(info: UaString): OperatingSystem {
            const current = info.current;
            const version = info.version;

            const isOS = (name: string) => (): boolean => current === name;

            return {
                current,
                version,

                isWindows: isOS(windows),
                isiOS: isOS(ios),
                isAndroid: isOS(android),
                isOSX: isOS(osx),
                isLinux: isOS(linux),
                isSolaris: isOS(solaris),
                isFreeBSD: isOS(freebsd),
                isChromeOS: isOS(chromeos)
            };
        };
    }

    export interface DeviceType {
        isiPad: () => boolean;
        isiPhone: () => boolean;
        isTablet: () => boolean;
        isPhone: () => boolean;
        isTouch: () => boolean;
        isAndroid: () => boolean;
        isiOS: () => boolean;
        isWebView: () => boolean;
        isDesktop: () => boolean;
    }

    export function DeviceType(os: OperatingSystem, browser: Browser, userAgent: string, mediaMatch: (query: string) => boolean): DeviceType {
        const isiPad = os.isiOS() && /ipad/i.test(userAgent) === true;
        const isiPhone = os.isiOS() && !isiPad;
        const isMobile = os.isiOS() || os.isAndroid();
        const isTouch = isMobile || mediaMatch('(pointer:coarse)');
        const isTablet = isiPad || !isiPhone && isMobile && mediaMatch('(min-device-width:768px)');
        const isPhone = isiPhone || isMobile && !isTablet;

        const iOSwebview = browser.isSafari() && os.isiOS() && /safari/i.test(userAgent) === false;
        const isDesktop = !isPhone && !isTablet && !iOSwebview;

        return {
            isiPad: () => isiPad,
            isiPhone: () => isiPhone,
            isTablet: () => isTablet,
            isPhone: () => isPhone,
            isTouch: () => isTouch,
            isAndroid: os.isAndroid,
            isiOS: os.isiOS,
            isWebView: () => iOSwebview,
            isDesktop: () => isDesktop
        };
    }

    export interface PlatformDetection {
        browser: Browser;
        os: OperatingSystem;
        deviceType: DeviceType;
    }

    export function detect(userAgent: string = navigator.userAgent, mediaMatch: (query: string) => boolean = (query) => window.matchMedia(query).matches): PlatformDetection {
        const browsers = PlatformInfo.browsers;
        const oses = PlatformInfo.oses;

        const browserInfo = UaString.detectBrowser(browsers, userAgent);
        const browser = browserInfo ? Browser.nu(browserInfo) : Browser.unknown();

        const osInfo = UaString.detectOs(oses, userAgent);
        const os = osInfo ? OperatingSystem.nu(osInfo) : OperatingSystem.unknown();

        const deviceType = DeviceType(os, browser, userAgent, mediaMatch);

        return {
            browser,
            os,
            deviceType
        };
    };
}

const PLATFORM = Platform.detect();
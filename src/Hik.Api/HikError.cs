using System.ComponentModel;

namespace Hik.Api
{
    internal enum HikError
    {
        [Description("No error.")]
        NET_DVR_NOERROR = 0,

        [Description("User name or password error.")]
        NET_DVR_PASSWORD_ERROR = 1,

        [Description("Not authorized to do this operation.")]
        NET_DVR_NOENOUGHPRI = 2,

        [Description("SDK is not initialized.")]
        NET_DVR_NOINIT = 3,

        [Description("Channel number error. There is no corresponding channel number on the device.")]
        NET_DVR_CHANNEL_ERROR = 4,

        [Description("The number of clients connected to the device has exceeded the max limit.")]
        NET_DVR_OVER_MAXLINK = 5,

        [Description("Version mismatch. SDK version is not matching with the device.")]
        NET_DVR_VERSIONNOMATCH = 6,

        [Description("Failed to connect to the device. The device is off-line, or connection timeout caused by network.")]
        NET_DVR_NETWORK_FAIL_CONNECT = 7,

        [Description("Failed to send data to the device.")]
        NET_DVR_NETWORK_SEND_ERROR = 8,

        [Description("Failed to receive data from the device.")]
        NET_DVR_NETWORK_RECV_ERROR = 9,

        [Description("Timeout when receiving the data from the device.")]
        NET_DVR_NETWORK_RECV_TIMEOUT = 10,

        [Description("The data sent to the device is illegal, or the data received from the device error. E.g. The input data is not supported by the device for remote configuration.")]
        NET_DVR_NETWORK_ERRORDATA = 11,

        [Description("API calling order error.")]
        NET_DVR_ORDER_ERROR = 12,

        [Description("Not authorized for this operation.")]
        NET_DVR_OPERNOPERMIT = 13,

        [Description("Executing command on the device is timeout.")]
        NET_DVR_COMMANDTIMEOUT = 14,

        [Description("Serial port number error. The assigned serial port does not exist on the device.")]
        NET_DVR_ERRORSERIALPORT = 15,

        [Description("Alarm port number error.")]
        NET_DVR_ERRORALARMPORT = 16,

        [Description("Parameter error. Input or output parameter in the SDK API is NULL.")]
        NET_DVR_PARAMETER_ERROR = 17,

        [Description("Device channel is in exception status.")]
        NET_DVR_CHAN_EXCEPTION = 18,

        [Description("No hard disk on the device, and the operation of recording and hard disk configuration will fail.")]
        NET_DVR_NODISK = 19,

        [Description("Hard disk number error. The assigned hard disk number does not exist during hard disk management.")]
        NET_DVR_ERRORDISKNUM = 20,

        [Description("Device hark disk is full.")]
        NET_DVR_DISK_FULL = 21,

        [Description("Device hard disk error.")]
        NET_DVR_DISK_ERROR = 22,

        [Description("Device does not support this function.")]
        NET_DVR_NOSUPPORT = 23,

        [Description("Device is busy.")]
        NET_DVR_BUSY = 24,

        [Description("Failed to modify device parameters.")]
        NET_DVR_MODIFY_FAIL = 25,

        [Description("The inputting password format is not correct.")]
        NET_DVR_PASSWORD_FORMAT_ERROR = 26,

        [Description("Hard disk is formatting, and the operation cannot be done.")]
        NET_DVR_DISK_FORMATING = 27,

        [Description("Not enough resource on the device.")]
        NET_DVR_DVRNORESOURCE = 28,

        [Description("Device operation failed.")]
        NET_DVR_DVROPRATEFAILED = 29,

        [Description("Failed to collect local audio data or to open audio output during voice talk / broadcasting.")]
        NET_DVR_OPENHOSTSOUND_FAIL = 30,

        [Description("Voice talk channel on the device has been occupied.")]
        NET_DVR_DVRVOICEOPENED = 31,

        [Description("Time input is not correct.")]
        NET_DVR_TIMEINPUTERROR = 32,

        [Description("There is no selected file for playback.")]
        NET_DVR_NOSPECFILE = 33,

        [Description("Failed to create a file, during local recording, saving picture, getting configuration file or downloading record file.")]
        NET_DVR_CREATEFILE_ERROR = 34,

        [Description("Failed to open a file, when importing configuration file, upgrading device or uploading inquest file.")]
        NET_DVR_FILEOPENFAIL = 35,

        [Description("The last operation has not been completed.")]
        NET_DVR_OPERNOTFINISH = 36,

        [Description("Failed to get the current played time.")]
        NET_DVR_GETPLAYTIMEFAIL = 37,

        [Description("Failed to start playback.")]
        NET_DVR_PLAYFAIL = 38,

        [Description("The file format is not correct.")]
        NET_DVR_FILEFORMAT_ERROR = 39,

        [Description("File directory error.")]
        NET_DVR_DIR_ERROR = 40,

        [Description("Resource allocation error.")]
        NET_DVR_ALLOC_RESOURCE_ERROR = 41,

        [Description("Sound adapter mode error. Currently opened sound playing mode does not match with the set mode.")]
        NET_DVR_AUDIO_MODE_ERROR = 42,

        [Description("Buffer is not enough.")]
        NET_DVR_NOENOUGH_BUF = 43,

        [Description("Create SOCKET error.")]
        NET_DVR_CREATESOCKET_ERROR = 44,

        [Description("Set SOCKET error.")]
        NET_DVR_SETSOCKET_ERROR = 45,

        [Description("The number of login or preview connections has exceeded the SDK limitation.")]
        NET_DVR_MAX_NUM = 46,

        [Description("User doest not exist. The user ID has been logged out or unavailable.")]
        NET_DVR_USERNOTEXIST = 47,

        [Description("Writing FLASH error. Failed to write FLASH during device upgrade.")]
        NET_DVR_WRITEFLASHERROR = 48,

        [Description("Failed to upgrade device. It is caused by network problem or the language mismatch between the device and the upgrade file.")]
        NET_DVR_UPGRADEFAIL = 49,

        [Description("The decode card has already been initialed.")]
        NET_DVR_CARDHAVEINIT = 50,

        [Description("Failed to call API of player SDK.")]
        NET_DVR_PLAYERFAILED = 51,

        [Description("The number of login user has reached the maximum limit.")]
        NET_DVR_MAX_USERNUM = 52,

        [Description("Failed to get the IP address or physical address of local PC.")]
        NET_DVR_GETLOCALIPANDMACFAIL = 53,

        [Description("This channel hasn't started encoding.")]
        NET_DVR_NOENCODEING = 54,

        [Description("IP address not match.")]
        NET_DVR_IPMISMATCH = 55,

        [Description("MAC address not match.")]
        NET_DVR_MACMISMATCH = 56,

        [Description("The language of upgrading file does not match the language of the device.")]
        NET_DVR_UPGRADELANGMISMATCH = 57,

        [Description("The number of player ports has reached the maximum limit.")]
        NET_DVR_MAX_PLAYERPORT = 58,

        [Description("No enough space to backup file in backup device.")]
        NET_DVR_NOSPACEBACKUP = 59,

        [Description("No backup device.")]
        NET_DVR_NODEVICEBACKUP = 60,

        [Description("The color quality setting of the picture does not match the requirement, and it should be limited to 24.")]
        NET_DVR_PICTURE_BITS_ERROR = 61,

        [Description("The dimension is over 128x256.")]
        NET_DVR_PICTURE_DIMENSION_ERROR = 62,

        [Description("The size of picture is over 100K.")]
        NET_DVR_PICTURE_SIZ_ERROR = 63,

        [Description("Failed to load the player SDK.")]
        NET_DVR_LOADPLAYERSDKFAILED = 64,

        [Description("Can not find the function in player SDK.")]
        NET_DVR_LOADPLAYERSDKPROC_ERROR = 65,

        [Description("Failed to load the library file-DsSdk.")]
        NET_DVR_LOADDSSDKFAILED = 66,

        [Description("Can not find the API in DsSdk.")]
        NET_DVR_LOADDSSDKPROC_ERROR = 67,

        [Description("Failed to call the API in DsSdk.")]
        NET_DVR_DSSDK_ERROR = 68,

        [Description("Sound adapter has been monopolized.")]
        NET_DVR_VOICEMONOPOLIZE = 69,

        [Description("Failed to join to multicast group.")]
        NET_DVR_JOINMULTICASTFAILED = 70,

        [Description("Failed to create log file directory.")]
        NET_DVR_CREATEDIR_ERROR = 71,

        [Description("Failed to bind socket.")]
        NET_DVR_BINDSOCKET_ERROR = 72,

        [Description("Socket disconnected. It is caused by network disconnection or destination unreachable.")]
        NET_DVR_SOCKETCLOSE_ERROR = 73,

        [Description("The user ID is operating when logout.")]
        NET_DVR_USERID_ISUSING = 74,

        [Description("Failed to listen.")]
        NET_DVR_SOCKETLISTEN_ERROR = 75,

        [Description("SDK program exception.")]
        NET_DVR_PROGRAM_EXCEPTION = 76,

        [Description("Failed to write file, during local recording, saving picture or downloading record file.")]
        NET_DVR_WRITEFILE_FAILED = 77,

        [Description("Failed to format read-only HD.")]
        NET_DVR_FORMAT_READONLY = 78,

        [Description("This user name already exists in the user configuration structure.")]
        NET_DVR_WITHSAMEUSERNAME = 79,

        [Description("Device type does not match when import configuration.")]
        NET_DVR_DEVICETYPE_ERROR = 80,

        [Description("Language does not match when import configuration.")]
        NET_DVR_LANGUAGE_ERROR = 81,

        [Description("Software version does not match when import configuration.")]
        NET_DVR_PARAVERSION_ERROR = 82,

        [Description("IP channel is not on-line when previewing.")]
        NET_DVR_IPCHAN_NOTALIVE = 83,

        [Description("Load StreamTransClient.dll failed.")]
        NET_DVR_RTSP_SDK_ERROR = 84,

        [Description("Load SystemTransform.dll failed.")]
        NET_DVR_CONVERT_SDK_ERROR = 85,

        [Description("Exceeds maximum number of connected IP channels.")]
        NET_DVR_IPC_COUNT_OVERFLOW = 86,

        [Description("Exceeds maximum number of supported record labels or other operations.")]
        NET_DVR_MAX_ADD_NUM = 87,

        [Description("Image intensifier, parameter mode error. This error may occur when client sets software or hardware parameters.")]
        NET_DVR_PARAMMODE_ERROR = 88,

        [Description("Code splitter is offline.")]
        NET_DVR_CODESPITTER_OFFLINE = 89,

        [Description("Device is backing up.")]
        NET_DVR_BACKUP_COPYING = 90,

        [Description("Channel not support.")]
        NET_DVR_CHAN_NOTSUPPORT = 91,

        [Description("The height line location is too concentrated, or the length line is not inclined enough.")]
        NET_DVR_CALLINEINVALID = 92,

        [Description("Cancel calibration conflict, if the rule and overall actual size filter have been set.")]
        NET_DVR_CALCANCELCONFLICT = 93,

        [Description("Calibration point exceeds the range.")]
        NET_DVR_CALPOINTOUTRANGE = 94,

        [Description("The size filter does not meet the requirement.")]
        NET_DVR_FILTERRECTINVALID = 95,

        [Description("Device has not registered to DDNS.")]
        NET_DVR_DDNS_DEVOFFLINE = 96,

        [Description("DDNS inner error.")]
        NET_DVR_DDNS_INTER_ERROR = 97,

        [Description("Alias is duplicate (for EasyDDNS)")]
        NET_DVR_ALIAS_DUPLICATE = 150,

        [Description("Network traffic is over device ability limit.")]
        NET_DVR_DEV_NET_OVERFLOW = 800,

        [Description("The video file is recording and can't be locked.")]
        NET_DVR_STATUS_RECORDFILE_WRITING_NOT_LOCK = 801,

        [Description("The hard disk capacity is too small and can not be formatted.")]
        NET_DVR_STATUS_CANT_FORMAT_LITTLE_DISK = 802,
    }
}
//
// Copyright (c) 2022 Winlin
//
// SPDX-License-Identifier: MIT
//
using System.Collections;
using UnityEngine;
using Unity.WebRTC;
using UnityEngine.UI;

// See https://docs.unity.cn/Packages/com.unity.webrtc@2.4/manual/tutorial.html
public class SrsPlayer : MonoBehaviour
{
    // The WHIP stream url, to pull WebRTC stream from SRS or other media
    // servers. Please note that SRS uses `/rtc/v1/whip-play/` as a WebRTC
    // player, or parameter `action=play` in query string.
    //public string url = "http://localhost:1985/rtc/v1/whip-play/?app=live&stream=livestream";
    public string url = " http://customer-pbwol7iolrvppuny.cloudflarestream.com/d92525c0fd1172344d68a52c500d0796/webRTC/play";
    // The RAW image to render the received WebRTC video stream. Generally, it
    // should be in a Canvas object. Please note that we will scale the image
    // size according to the video stream resolution.
    public UnityEngine.UI.RawImage receiveImage;
    //public RawImage receiveImage;
    // The audio source to play the received WebRTC audio stream. Please create
    // a normal audio source.
    public AudioSource receiveAudio;

    private MediaStream receiveStream;
    private RTCPeerConnection pc;

    private void Awake()
    {
#if WEBRTC_3_0_0_PRE_5_OR_BEFORE
        WebRTC.Initialize();
#endif
        Debug.Log("WebRTC: Initialize ok");
    }

    private void OnDestroy()
    {
        pc?.Close();
        pc?.Dispose();
        pc = null;

#if WEBRTC_3_0_0_PRE_5_OR_BEFORE
        WebRTC.Dispose();
#endif
        Debug.Log("WebRTC: Dispose ok");
    }

    private void Start()
    {
        Debug.Log($"WebRTC: Start to play {url}");

        // Start WebRTC update loop
        StartCoroutine(WebRTC.Update());

        // Create peer connection
        pc = new RTCPeerConnection();

        // Setup ICE callbacks
        pc.OnIceCandidate = candidate =>
        {
            Debug.Log($"WebRTC: OnIceCandidate {candidate}");
        };
        pc.OnIceConnectionChange = state =>
        {
            Debug.Log($"WebRTC: OnIceConnectionChange {state}");
        };

        // Handle incoming tracks directly here
        pc.OnTrack = e =>
        {
            Debug.Log($"Track kind: {e.Track.Kind}, id: {e.Track.Id}");

            if (e.Track is VideoStreamTrack videoTrack)
            {
                Debug.Log("WebRTC: Video track received");

                videoTrack.OnVideoReceived += tex =>
                {
                    Debug.Log($"WebRTC: OnVideoReceived {tex.width}x{tex.height}");

                    receiveImage.texture = tex; // Directly assign
                    receiveImage.enabled = true;

                    //Set image size based on video
                    var width = tex.width < 1280 ? tex.width : 1280;
                    var height = tex.width > 0 ? width * tex.height / tex.width : 720;
                    //receiveImage.rectTransform.sizeDelta = new Vector2(width, height);
                    receiveImage.rectTransform.sizeDelta = new Vector2(3, 3);
                    receiveImage.rectTransform.localScale = Vector3.one;
                    //receiveImage.texture.filterMode = FilterMode.Bilinear;
                    //receiveImage.texture.wrapMode = TextureWrapMode.Clamp;

                };
            }

            if (e.Track is AudioStreamTrack audioTrack)
            {
                Debug.Log($"WebRTC: Audio track received");
                receiveAudio.SetTrack(audioTrack);
                receiveAudio.loop = true;
                receiveAudio.Play();
            }
        };

        // Setup receiving transceivers
        StartCoroutine(SetupPeerConnection());
    }

    IEnumerator SetupPeerConnection()
    {
        RTCRtpTransceiverInit init = new RTCRtpTransceiverInit
        {
            direction = RTCRtpTransceiverDirection.RecvOnly
        };
        pc.AddTransceiver(TrackKind.Audio, init);
        pc.AddTransceiver(TrackKind.Video, init);

        yield return StartCoroutine(PeerNegotiationNeeded());
    }

    IEnumerator PeerNegotiationNeeded()
    {
        var op = pc.CreateOffer();
        yield return op;

        Debug.Log($"WebRTC: CreateOffer done={op.IsDone}, hasError={op.IsError}");
        if (op.IsError) yield break;

        yield return StartCoroutine(OnCreateOfferSuccess(op.Desc));
    }

    IEnumerator OnCreateOfferSuccess(RTCSessionDescription offer)
    {
        var op = pc.SetLocalDescription(ref offer);
        Debug.Log($"WebRTC: SetLocalDescription {offer.type} {offer.sdp}");
        yield return op;

        if (op.IsError) yield break;

        yield return StartCoroutine(ExchangeSDP(url, offer.sdp));
    }

    IEnumerator ExchangeSDP(string url, string offer)
    {
        var task = System.Threading.Tasks.Task<string>.Run(async () =>
        {
            System.Uri uri = new System.UriBuilder(url).Uri;
            var content = new System.Net.Http.StringContent(offer);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/sdp");

            var client = new System.Net.Http.HttpClient();
            var res = await client.PostAsync(uri, content);
            res.EnsureSuccessStatusCode();

            return await res.Content.ReadAsStringAsync();
        });

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.Log($"WebRTC: Exchange SDP failed, url={url}, err={task.Exception}");
            yield break;
        }

        yield return StartCoroutine(OnGotAnswerSuccess(task.Result));
    }

    IEnumerator OnGotAnswerSuccess(string answer)
    {
        RTCSessionDescription desc = new RTCSessionDescription
        {
            type = RTCSdpType.Answer,
            sdp = answer
        };
        var op = pc.SetRemoteDescription(ref desc);
        yield return op;

        Debug.Log($"WebRTC: Answer done={op.IsDone}, hasError={op.IsError}");
    }
    private void Update()
    {
    }
}

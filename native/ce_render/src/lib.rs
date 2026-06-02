//! Native ToAnsiString for ConsoleEngine.Rendering (L1 PoC).
//!
//! Produces byte-identical output to the C# `PixelArtRenderer.BuildAnsiRows` half-block encoding,
//! written straight into a caller-provided buffer (no allocation on the Rust side). UTF-8 bytes;
//! the C# side decodes with `Encoding.UTF8.GetString` (giving the same chars, incl. ▀ ▄).
//!
//! Encoding per cell (top pixel = fg, bottom pixel = bg):
//!   both solid : ESC[38;2;tR;tG;tBm ESC[48;2;bR;bG;bBm ▀
//!   top only   : ESC[38;2;tR;tG;tBm ESC[49m ▀
//!   bottom only: ESC[38;2;bR;bG;bBm ESC[49m ▄
//!   neither    : ESC[0m <space>
//! Each terminal row ends with ESC[0m; rows are joined by "\n" or "\r\n".

use std::slice;

#[inline(always)]
fn push_u8_dec(buf: &mut [u8], pos: usize, v: u8) -> usize {
    if v >= 100 {
        buf[pos] = b'0' + v / 100;
        buf[pos + 1] = b'0' + (v / 10) % 10;
        buf[pos + 2] = b'0' + v % 10;
        pos + 3
    } else if v >= 10 {
        buf[pos] = b'0' + v / 10;
        buf[pos + 1] = b'0' + v % 10;
        pos + 2
    } else {
        buf[pos] = b'0' + v;
        pos + 1
    }
}

#[inline(always)]
fn push(buf: &mut [u8], pos: usize, src: &[u8]) -> usize {
    buf[pos..pos + src.len()].copy_from_slice(src);
    pos + src.len()
}

const UPPER_HALF: &[u8] = b"\xe2\x96\x80"; // ▀ U+2580
const LOWER_HALF: &[u8] = b"\xe2\x96\x84"; // ▄ U+2584

/// Convert an RGB pixel grid (row-major, 3 bytes/pixel) to a half-block ANSI byte string.
///
/// Returns: 0 = ok (`*out_len` = bytes written); 1 = `out_buf` too small or null
/// (`*out_len` = required capacity — call again with a buffer that large); <0 = bad args.
/// `crlf` != 0 joins rows with "\r\n", else "\n". A pixel equal to (tr_r,tr_g,tr_b) is transparent.
///
/// # Safety
/// `pixels` must point to `width*height*3` readable bytes; `out_buf` to `out_cap` writable bytes;
/// `out_len` must be a valid `*mut usize`.
#[no_mangle]
pub unsafe extern "C" fn ce_pixels_to_ansi(
    pixels: *const u8,
    width: i32,
    height: i32,
    tr_r: u8,
    tr_g: u8,
    tr_b: u8,
    crlf: i32,
    out_buf: *mut u8,
    out_cap: usize,
    out_len: *mut usize,
) -> i32 {
    if pixels.is_null() || out_len.is_null() || width <= 0 || height <= 0 {
        return -1;
    }
    let w = width as usize;
    let h = height as usize;
    let px = slice::from_raw_parts(pixels, w * h * 3);
    let term_rows = (h + 1) / 2;
    let nl_len = if crlf != 0 { 2 } else { 1 };

    // True upper bound: worst cell = 41 bytes; per row tail = ESC[0m (4) + newline.
    let max_needed = term_rows * (w * 41 + 4 + nl_len);
    if out_buf.is_null() || out_cap < max_needed {
        *out_len = max_needed;
        return 1;
    }
    let buf = slice::from_raw_parts_mut(out_buf, out_cap);

    let solid = |idx3: usize| -> bool {
        px[idx3] != tr_r || px[idx3 + 1] != tr_g || px[idx3 + 2] != tr_b
    };

    let mut pos = 0usize;
    let mut y = 0usize;
    while y < h {
        let row = y * w;
        let row1 = (y + 1) * w;
        let has_bot_row = y + 1 < h;
        for x in 0..w {
            let ti = (row + x) * 3;
            let top_ok = solid(ti);
            let bi = (row1 + x) * 3;
            let bot_ok = has_bot_row && solid(bi);

            if !top_ok && !bot_ok {
                pos = push(buf, pos, b"\x1b[0m ");
            } else if top_ok && bot_ok {
                pos = push(buf, pos, b"\x1b[38;2;");
                pos = push_u8_dec(buf, pos, px[ti]); buf[pos] = b';'; pos += 1;
                pos = push_u8_dec(buf, pos, px[ti + 1]); buf[pos] = b';'; pos += 1;
                pos = push_u8_dec(buf, pos, px[ti + 2]);
                pos = push(buf, pos, b"m\x1b[48;2;");
                pos = push_u8_dec(buf, pos, px[bi]); buf[pos] = b';'; pos += 1;
                pos = push_u8_dec(buf, pos, px[bi + 1]); buf[pos] = b';'; pos += 1;
                pos = push_u8_dec(buf, pos, px[bi + 2]);
                buf[pos] = b'm'; pos += 1;
                pos = push(buf, pos, UPPER_HALF);
            } else if top_ok {
                pos = push(buf, pos, b"\x1b[38;2;");
                pos = push_u8_dec(buf, pos, px[ti]); buf[pos] = b';'; pos += 1;
                pos = push_u8_dec(buf, pos, px[ti + 1]); buf[pos] = b';'; pos += 1;
                pos = push_u8_dec(buf, pos, px[ti + 2]);
                pos = push(buf, pos, b"m\x1b[49m");
                pos = push(buf, pos, UPPER_HALF);
            } else {
                pos = push(buf, pos, b"\x1b[38;2;");
                pos = push_u8_dec(buf, pos, px[bi]); buf[pos] = b';'; pos += 1;
                pos = push_u8_dec(buf, pos, px[bi + 1]); buf[pos] = b';'; pos += 1;
                pos = push_u8_dec(buf, pos, px[bi + 2]);
                pos = push(buf, pos, b"m\x1b[49m");
                pos = push(buf, pos, LOWER_HALF);
            }
        }
        pos = push(buf, pos, b"\x1b[0m");
        if y + 2 < h {
            pos = push(buf, pos, if crlf != 0 { b"\r\n" } else { b"\n" });
        }
        y += 2;
    }

    *out_len = pos;
    0
}

/*
  Copyright @ 2003 ImageMagick Studio LLC, a non-profit organization
  dedicated to making software imaging solutions freely available.
  
  You may not use this file except in compliance with the License.  You may
  obtain a copy of the License at
  
    https://imagemagick.org/script/license.php
  
  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.

  MagickCore private locale methods.
*/
#ifndef MAGICKCORE_LOCALE_PRIVATE_H
#define MAGICKCORE_LOCALE_PRIVATE_H

#if defined(__cplusplus) || defined(c_plusplus)
extern "C" {
#endif

#if !defined(MagickLocaleExtent)
# define MagickLocaleExtent  256
#endif

extern MagickPrivate MagickBooleanType
  LocaleComponentGenesis(void);

extern MagickPrivate void
  LocaleComponentTerminus(void);

extern MagickPrivate ssize_t
  FormatLocaleFileList(FILE *,const char *magick_restrict,va_list)
    magick_attribute((__format__ (__printf__,2,0))),
  FormatLocaleStringList(char *magick_restrict,const size_t,
    const char *magick_restrict,va_list)
    magick_attribute((__format__ (__printf__,3,0)));

static inline int LocaleToLowercase(const int c)
{
  if ((c == EOF) || (c != (unsigned char) c))
    return(c);
#if defined(MAGICKCORE_LOCALE_SUPPORT)
  if (c_locale != (locale_t) NULL)
    return(tolower_l((int) ((unsigned char) c),c_locale));
#endif
  return(tolower((int) ((unsigned char) c)));
}

static inline int LocaleToUppercase(const int c)
{
  if ((c == EOF) || (c != (unsigned char) c))
    return(c);
#if defined(MAGICKCORE_LOCALE_SUPPORT)
  if (c_locale != (locale_t) NULL)
    return(toupper_l((int) ((unsigned char) c),c_locale));
#endif
  return(toupper((int) ((unsigned char) c)));
}

#if defined(__cplusplus) || defined(c_plusplus)
}
#endif

#endif
